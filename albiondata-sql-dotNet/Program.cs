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
    public static string NatsUrl { get; } = "nats://public:thenewalbiondata@localhost:4222";

    [Option(Description = "SQL Connection Url", ShortName = "s", ShowInHelpText = true)]
    public static string SqlConnectionUrl { get; } = "SslMode=none;server=localhost;port=3306;database=albion;user=root;password=";

    [Option(Description = "Check Every x Minutes for expired orders", ShortName = "e", ShowInHelpText = true)]
    [Range(1, 120)]
    public static int ExpireCheckMinutes { get; } = 10;

    [Option(Description = "Max age in Days that orders exist before deletion", ShortName = "a", ShowInHelpText = true)]
    [Range(1, 30)]
    public static int MaxAgeDays { get; } = 3;

    [Option(Description = "Enable Debug Logging", ShortName = "d", LongName = "debug", ShowInHelpText = true)]
    public static bool Debug { get; }

    public static ILoggerFactory LoggerFactory { get; } = new LoggerFactory().AddConsole(Debug ? LogLevel.Debug : LogLevel.Information);
    public static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();

    private static readonly ManualResetEvent quitEvent = new ManualResetEvent(false);

    private static ulong updatedCounter = 0;

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
      var incomingGoldData = NatsConnection.SubscribeAsync(goldDataDeduped);

      incomingMarketOrders.MessageHandler += HandleMarketOrder;
      incomingGoldData.MessageHandler += HandleGoldData;

      incomingMarketOrders.Start();
      logger.LogInformation("Listening for Market Order Data");
      incomingGoldData.Start();
      logger.LogInformation("Listening for Gold Data");

      var expireTimer = new Timer(ExpireOrders, null, TimeSpan.Zero, TimeSpan.FromMinutes(ExpireCheckMinutes));

      quitEvent.WaitOne();
      NatsConnection.Close();
    }

    private static void HandleMarketOrder(object sender, MsgHandlerEventArgs args)
    {
      var logger = CreateLogger<Program>();
      var message = args.Message;
      try
      {
        using (var context = new ConfiguredContext())
        {
          var marketOrder = JsonConvert.DeserializeObject<MarketOrderDB>(Encoding.UTF8.GetString(message.Data));
          marketOrder.AlbionId = marketOrder.Id;
          marketOrder.Id = 0;
          var dbOrder = context.MarketOrders.FirstOrDefault(x => x.AlbionId == marketOrder.AlbionId);
          if (dbOrder != null)
          {
            //Console.WriteLine($"Updating Market: Price:{marketOrder.UnitPriceSilver} - {marketOrder}");
            // Update UnitPriceSilver until 2019 so all the remnants of the deduper issue can be resolved
            dbOrder.UnitPriceSilver = marketOrder.UnitPriceSilver;
            dbOrder.UpdatedAt = DateTime.UtcNow;
            dbOrder.Amount = marketOrder.Amount;
            dbOrder.LocationId = marketOrder.LocationId;
            dbOrder.DeletedAt = null;
            context.MarketOrders.Update(dbOrder);
          }
          else
          {
            //Console.WriteLine($"Creating Market: Price:{marketOrder.UnitPriceSilver} - {marketOrder}");
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
          updatedCounter++;
          if (updatedCounter % 100 == 0) logger.LogInformation($"Updated/Created {updatedCounter} Market Orders");
        }
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error handling market order");
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
        using (var context = new ConfiguredContext())
        {
          const int batchSize = 10000;
          var incrementalCount = 0;
          var totalCount = 0;
          var changesLeft = true;
          while (changesLeft)
          {
            changesLeft = false;
            incrementalCount = context.Database.ExecuteSqlCommand(@"UPDATE market_orders m
SET m.deleted_at = UTC_DATE()
WHERE m.deleted_at IS NULL
AND
(
m.expires < UTC_DATE()
OR
m.updated_at < DATE_ADD(UTC_DATE(),INTERVAL -{0} DAY)
)
LIMIT {1}", MaxAgeDays, batchSize);
            totalCount += incrementalCount;

            logger.LogInformation($"Expired {incrementalCount} orders...");
            if (incrementalCount == batchSize)
            {
              changesLeft = true;
            }
            Thread.Sleep(10000);
            // We have been deleting for too long, kill this thread
            if ((DateTime.Now - start).TotalMinutes > ExpireCheckMinutes * 0.75)
            {
              changesLeft = false;
            }
          }
          logger.LogInformation($"{totalCount} orders expired");
        }
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
        if (upload.Prices.Length != upload.TimeStamps.Length) throw new Exception("Different list lengths");
        using (var context = new ConfiguredContext())
        {
          for (var i = 0; i < upload.Prices.Length; i++)
          {
            var price = upload.Prices[i];
            var timestamp = new DateTime(upload.TimeStamps[i], DateTimeKind.Utc);
            var dbGold = context.GoldPrices.FirstOrDefault(x => x.TimeStamp == timestamp);
            if (dbGold != null)
            {
              if (dbGold.Price != price)
              {
                dbGold.UpdatedAt = DateTime.UtcNow;
                dbGold.Price = price;
                context.GoldPrices.Update(dbGold);
              }
            }
            else
            {
              var goldPrice = new GoldPrice()
              {
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Price = price,
                TimeStamp = timestamp
              };
              context.GoldPrices.Add(dbGold);
            }
          }
          context.SaveChanges();
        }
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error handling gold data");
      }
    }
  }
}
