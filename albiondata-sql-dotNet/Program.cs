using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NATS.Client;
using Newtonsoft.Json;
using System;
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

    [Option(Description = "Enable Debug Logging", ShortName = "d", LongName = "debug", ShowInHelpText = true)]
    public static bool Debug { get; }

    public static ILoggerFactory LoggerFactory { get; } = new LoggerFactory().AddConsole(Debug ? LogLevel.Debug : LogLevel.Information);
    public static ILogger CreateLogger<T>() => LoggerFactory.CreateLogger<T>();

    private static readonly ManualResetEvent quitEvent = new ManualResetEvent(false);

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
    private const string mapDataDeduped = "mapdata.deduped";
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

      using (var context = new MarketOrderContext())
      {
        if (context.Database.EnsureCreated())
        {
          logger.LogInformation("Database Created");
        }
        else
        {
          logger.LogInformation("Database Exists");
        }
      }

      logger.LogInformation($"Nats URL: {NatsUrl}");
      logger.LogInformation($"NATS Connected, ID: {NatsConnection.ConnectedId}");

      var incomingMarketOrders = NatsConnection.SubscribeAsync(marketOrdersDeduped);
      var incomingMapData = NatsConnection.SubscribeAsync(mapDataDeduped);
      var incomingGoldData = NatsConnection.SubscribeAsync(goldDataDeduped);

      incomingMarketOrders.MessageHandler += HandleMarketOrder;
      incomingMapData.MessageHandler += HandleMapData;
      incomingGoldData.MessageHandler += HandleGoldData;

      incomingMarketOrders.Start();
      logger.LogInformation("Listening for Market Order Data");
      incomingMapData.Start();
      logger.LogInformation("Listening for Map Data");
      incomingGoldData.Start();
      logger.LogInformation("Listening for Gold Data");

      quitEvent.WaitOne();
      NatsConnection.Close();
    }

    private static void HandleMarketOrder(object sender, MsgHandlerEventArgs args)
    {
      var logger = CreateLogger<Program>();
      var message = args.Message;
      try
      {
        using (var context = new MarketOrderContext())
        {
          var marketOrder = JsonConvert.DeserializeObject<MarketOrderDB>(Encoding.UTF8.GetString(message.Data));
          marketOrder.AlbionId = marketOrder.Id;
          marketOrder.Id = 0;
          var dbOrder = context.MarketOrders.FirstOrDefault(x => x.AlbionId == marketOrder.AlbionId);
          if (dbOrder != null)
          {
            logger.LogInformation($"Updating Market Order: {marketOrder}");
            dbOrder.UpdatedAt = DateTime.UtcNow;
            dbOrder.Amount = marketOrder.Amount;
            dbOrder.LocationId = marketOrder.LocationId;
            dbOrder.DeletedAt = null;
            context.MarketOrders.Update(dbOrder);
          }
          else
          {
            logger.LogInformation($"Creating Market Order: {marketOrder}");
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
        }
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error handling market order");
      }
    }

    private static void HandleMapData(object sender, MsgHandlerEventArgs args)
    {
      var logger = CreateLogger<Program>();
      var message = args.Message;
      try
      {
        logger.LogInformation("Processing Map Data");
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error handling map data");
      }
    }

    private static void HandleGoldData(object sender, MsgHandlerEventArgs args)
    {
      var logger = CreateLogger<Program>();
      var message = args.Message;
      try
      {
        logger.LogInformation("Processing Gold Data");
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error handling gold data");
      }
    }
  }
}
