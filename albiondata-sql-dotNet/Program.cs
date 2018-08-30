using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using NATS.Client;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;

namespace albiondata_sql_dotNet
{
  internal class Program
  {
    private static void Main(string[] args) => CommandLineApplication.Execute<Program>(args);

    [Option(Description = "Incoming NATS Url", ShortName = "n", ShowInHelpText = true)]
    public static string NatsUrl { get; } = "nats://public:thenewalbiondata@localhost:4222";

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
      logger.LogInformation($"Nats URL: {NatsUrl}");
      if (Debug)
        logger.LogInformation("Debugging enabled");

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
        var marketUpload = JsonConvert.DeserializeObject<MarketOrder>(Encoding.UTF8.GetString(message.Data));
        logger.LogInformation("Processing Market Order");
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
