using AlbionData.Models;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NATS.Client;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace albiondata_sql_dotNet
{
  internal class Program
  {
    private static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);

    [Option(Description = "NATS Url", ShortName = "n", ShowInHelpText = true)]
    public static string NatsUrl { get; set; } = "nats://public:thenewalbiondata@albion-online-data.com:4222";

    [Option(Description = "SQL Connection Url", ShortName = "s", ShowInHelpText = true)]
    public static string SqlConnectionUrl { get; set; } = "server=localhost;port=3306;database=albion;user=root;password=";

    [Option(Description = "Check Every x Minutes for expired orders", ShortName = "e", ShowInHelpText = true)]
    [Range(1, 1440)]
    public static int ExpireCheckMinutes { get; set; } = 60;

    [Option(Description = "Max age in Hours that orders exist before deletion", ShortName = "a", ShowInHelpText = true)]
    [Range(1, 168)]
    public static int MaxAgeHours { get; set; } = 24;

    [Option(Description = "Enable Debug Logging", ShortName = "d", LongName = "debug", ShowInHelpText = true)]
    public static bool Debug { get; set; }

    public static ILoggerFactory Logger { get; } = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(Debug ? LogLevel.Debug : LogLevel.Information));
    public static ILogger CreateLogger<T>() => Logger.CreateLogger<T>();

    private static readonly ManualResetEvent quitEvent = new ManualResetEvent(false);

    private static ulong updatedOrderCounter = 0;
    private static ulong updatedHistoryCounter = 0;

    private static readonly Timer expireTimer = new Timer(ExpireOrders, null, Timeout.Infinite, Timeout.Infinite);

    #region Connections
    private static readonly Lazy<IConnection> lazyNats = new Lazy<IConnection>(() =>
    {
      var natsFactory = new ConnectionFactory();
      return natsFactory.CreateConnection(NatsUrl);
    });

    public static IConnection NatsConnection
    {
      get
      {
        return lazyNats.Value;
      }
    }
    #endregion
    #region Subjects
    private const string marketOrdersDeduped = "marketorders.deduped";
    private const string marketHistoriesDeduped = "markethistories.deduped";
    private const string goldDataDeduped = "goldprices.deduped";
    #endregion

    private void OnExecute()
    {
      Console.CancelKeyPress += (sender, args) =>
      {
        quitEvent.Set();
        args.Cancel = true;
      };

      var logger = CreateLogger<Program>();
      if (Debug)
        logger.LogInformation("Debugging enabled");

      using (var context = new ConfiguredContext())
      {
        if (context.Database.EnsureCreated())
        {
          logger.LogInformation("Database Created");
          context.SaveChanges();
        }
        else
        {
          logger.LogInformation("Database Exists");
        }
      }

      logger.LogInformation($"Nats URL: {NatsUrl}");
      logger.LogInformation($"NATS Connected, ID: {NatsConnection.ConnectedId}");

      var incomingMarketOrders = NatsConnection.SubscribeAsync(marketOrdersDeduped);
      var incomingMarketHistories = NatsConnection.SubscribeAsync(marketHistoriesDeduped);
      var incomingGoldData = NatsConnection.SubscribeAsync(goldDataDeduped);

      incomingMarketOrders.MessageHandler += HandleMarketOrder;
      incomingMarketHistories.MessageHandler += HandleMarketHistory;
      incomingGoldData.MessageHandler += HandleGoldData;

      incomingMarketOrders.Start();
      logger.LogInformation("Listening for Market Order Data");
      incomingMarketHistories.Start();
      logger.LogInformation("Listening for Market History Data");
      incomingGoldData.Start();
      logger.LogInformation("Listening for Gold Data");

      logger.LogInformation($"Checking Every {ExpireCheckMinutes} Minutes for expired orders.");
      logger.LogInformation($"Deleting orders after {MaxAgeHours} hours");

      expireTimer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(ExpireCheckMinutes));

      quitEvent.WaitOne();
      NatsConnection.Close();
    }

    private static void HandleMarketOrder(object sender, MsgHandlerEventArgs args)
    {
      var logger = CreateLogger<Program>();
      var message = args.Message;
      try
      {
        using var context = new ConfiguredContext();
        var marketOrder = JsonConvert.DeserializeObject<MarketOrderDB>(Encoding.UTF8.GetString(message.Data));
        marketOrder.AlbionId = marketOrder.Id;
        marketOrder.Id = 0;
        var dbOrder = context.MarketOrders.FirstOrDefault(x => x.AlbionId == marketOrder.AlbionId);
        if (dbOrder != null)
        {
          dbOrder.UnitPriceSilver = marketOrder.UnitPriceSilver;
          dbOrder.UpdatedAt = DateTime.UtcNow;
          dbOrder.Amount = marketOrder.Amount;
          dbOrder.LocationId = marketOrder.LocationId;
          dbOrder.DeletedAt = null;
          context.MarketOrders.Update(dbOrder);
        }
        else
        {
          marketOrder.InitialAmount = marketOrder.Amount;
          marketOrder.CreatedAt = DateTime.UtcNow;
          marketOrder.UpdatedAt = DateTime.UtcNow;
          if (marketOrder.Expires > DateTime.UtcNow.AddYears(1))
          {
            marketOrder.Expires = DateTime.UtcNow.AddDays(7);
          }
          context.MarketOrders.Add(marketOrder);
        }
        context.SaveChanges();
        updatedOrderCounter++;
        if (updatedOrderCounter % 100 == 0) logger.LogInformation($"Updated/Created {updatedOrderCounter} Market Orders");
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error handling market order");
      }
    }

    private static void HandleMarketHistory(object sender, MsgHandlerEventArgs args)
    {
      var logger = CreateLogger<Program>();
      var message = args.Message;
      try
      {
        using var context = new ConfiguredContext();
        var upload = JsonConvert.DeserializeObject<MarketHistoriesUpload>(Encoding.UTF8.GetString(message.Data));
        var aggregationType = TimeAggregation.QuarterDay;
        if (upload.Timescale == Timescale.Day)
        {
          aggregationType = TimeAggregation.Hourly;
        }

        // Do not use the last history timestamp because it is a partial period
        // It is not guaranteed to be updated so it can appear that the count in the period was way lower
        foreach (var history in upload.MarketHistories.OrderBy(x => x.Timestamp).SkipLast(1))
        {
          var historyDate = new DateTime((long)history.Timestamp);
          var dbHistory = context.MarketHistories.FirstOrDefault(x =>
          x.ItemTypeId == upload.AlbionIdString
          && x.Location == upload.LocationId
          && x.QualityLevel == upload.QualityLevel
          && x.Timestamp == historyDate
          && x.AggregationType == aggregationType);

          if (dbHistory == null)
          {
            dbHistory = new MarketHistoryDB
            {
              AggregationType = aggregationType,
              ItemTypeId = upload.AlbionIdString,
              Location = upload.LocationId,
              QualityLevel = upload.QualityLevel,
              ItemAmount = history.ItemAmount,
              SilverAmount = history.SilverAmount,
              Timestamp = historyDate
            };
            context.MarketHistories.Add(dbHistory);
          }
          else
          {
            dbHistory.ItemAmount = history.ItemAmount;
            dbHistory.SilverAmount = history.SilverAmount;
            context.MarketHistories.Update(dbHistory);
          }
          updatedHistoryCounter++;
          if (updatedHistoryCounter % 100 == 0) logger.LogInformation($"Updated/Created {updatedHistoryCounter} Market Histories");
        }
        context.SaveChanges();
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error handling market history");
      }
    }

    private static void ExpireOrders(object state)
    {
      try
      {
        var start = DateTime.Now;
        File.AppendAllText("last-run.txt", start.ToString("F") + Environment.NewLine);

        var logger = CreateLogger<Program>();
        logger.LogInformation("Checking for expired orders");
        using var context = new ConfiguredContext();
        const int batchSize = 50000;
        var sleepTime = TimeSpan.FromSeconds(10);
        var incrementalCount = 0;
        var totalCount = 0;
        var prevTotalCount = totalCount;
        var changesLeft = true;
        while (changesLeft)
        {
          changesLeft = false;
          // Delete old market orders
          incrementalCount = context.Database.ExecuteSqlInterpolated(@$"DELETE m
FROM market_orders m
WHERE m.deleted_at IS NULL
AND
(
m.expires < UTC_TIMESTAMP()
OR
m.updated_at < DATE_ADD(UTC_TIMESTAMP(),INTERVAL -{MaxAgeHours} HOUR)
)");
          logger.LogInformation($"Deleted {incrementalCount} order records");
          totalCount += incrementalCount;
          Thread.Sleep(sleepTime);

          // Delete old hourly history records
          incrementalCount = context.Database.ExecuteSqlInterpolated(@$"DELETE m
FROM market_history m
WHERE m.aggregation = 1
AND m.timestamp < DATE_ADD(UTC_TIMESTAMP(),INTERVAL -7 DAY)");
          logger.LogInformation($"Deleted {incrementalCount} history records");
          totalCount += incrementalCount;
          Thread.Sleep(sleepTime);

          // Keep expiring when we are expiring large numbers at a time
          // Stop expiring when at fewer numbers or we will keep expiring forever
          if (incrementalCount > batchSize / 2)
          {
            changesLeft = true;
          }
          // We have been deleting for too long, kill this thread
          if ((DateTime.Now - start).TotalMinutes > ExpireCheckMinutes * 0.75)
          {
            logger.LogInformation("Killing long running thread");
            changesLeft = false;
          }
          prevTotalCount = totalCount;
        }
        logger.LogInformation($"{totalCount} total updates");
      }
      catch (Exception ex)
      {
        File.AppendAllText("last-run.txt", DateTime.Now.ToString("F") + Environment.NewLine + ex.ToString() + Environment.NewLine);
      }
    }

    private static void HandleGoldData(object sender, MsgHandlerEventArgs args)
    {
      var logger = CreateLogger<Program>();
      var message = args.Message;
      try
      {
        logger.LogInformation("Processing Gold Data");
        var upload = JsonConvert.DeserializeObject<GoldPriceUpload>(Encoding.UTF8.GetString(message.Data));
        if (upload.Prices.Length != upload.Timestamps.Length) throw new Exception("Different list lengths");
        using var context = new ConfiguredContext();
        for (var i = 0; i < upload.Prices.Length; i++)
        {
          var price = upload.Prices[i];
          var timestamp = new DateTime(upload.Timestamps[i], DateTimeKind.Utc);
          var dbGold = context.GoldPrices.FirstOrDefault(x => x.Timestamp == timestamp);
          if (dbGold != null)
          {
            if (dbGold.Price != price)
            {
              dbGold.Price = price;
              context.GoldPrices.Update(dbGold);
            }
          }
          else
          {
            var goldPrice = new GoldPrice()
            {
              Price = price,
              Timestamp = timestamp
            };
            context.GoldPrices.Add(goldPrice);
          }
        }
        context.SaveChanges();
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error handling gold data");
      }
    }
  }
}
