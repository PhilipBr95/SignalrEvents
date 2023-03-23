using ClassLibrary1;
using Microsoft.Extensions.Logging;
using TPT.Notification.NotifierLibrary;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ConsoleApp
{
    internal class Program
    {
        private static ILoggerFactory _loggerFactory;
        private static ILogger<Program> _logger;

        static async Task Main(string[] args)
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
            });

            _logger = _loggerFactory.CreateLogger<Program>();

            _logger.LogInformation("Press Enter to connect");
            Console.ReadLine();

            var jsonSerialiser = new MySerialiser();
            await CreateCalc_ClientAsync("CalcClient 1", jsonSerialiser);
            await CreateTree_ClientAsync("TreeClient 1", jsonSerialiser);
            await CreateServerAsync(jsonSerialiser);

        }

        public class MySerialiser : INotifierSerialiser
        {
            public object? Deserialise(string? json, Type returnType)
            {
                return JsonSerializer.Deserialize(json, returnType, new JsonSerializerOptions {  PropertyNameCaseInsensitive = true });
            }

            public string Serialise(object? obj)
            {
                return JsonSerializer.Serialize(obj);
            }
        }

        private static async Task CreateCalc_ClientAsync(string loggerName, INotifierSerialiser jsonSerialiser) 
        {
            var notifier = new Notifier<CalculationNotifications>(new NotifierSettings("https://localhost:7062/NotificationHub", NotifierPurpose.Receiver, jsonSerialiser, false), _loggerFactory.CreateLogger(loggerName));
            var notifications = await notifier.ConnectAsync();

            notifications.Started += (object? sender, StartedEventArgs e) =>
            {
                var objSender = jsonSerialiser.Deserialise(sender.ToString(), typeof(FakeSender)) as FakeSender;
                _logger.LogInformation($"{loggerName}: Received {nameof(notifications.Started)} for {e.TargetId} from {objSender.SenderId}[Converted]");
            };

            notifications.Finished += (object? sender, FinishedEventArgs e) =>
            {
                _logger.LogInformation($"{loggerName}: Received {nameof(notifications.Finished)} for {e.TargetId} from {sender}");
            };
        }

        //todo - how do we know something hasn't gone wrong and we've missed the event

        private static async Task CreateTree_ClientAsync(string loggerName, INotifierSerialiser jsonSerialiser)
        {       
            var notifier = new Notifier<TreeNotifications>(new NotifierSettings("https://localhost:7062/NotificationHub", NotifierPurpose.Receiver, jsonSerialiser, false), _loggerFactory.CreateLogger(loggerName));
            var notifications = await notifier.ConnectAsync();

            notifications.Started += (object? sender, StartedEventArgs e) =>
            {
                _logger.LogInformation($"{loggerName}: Received {nameof(notifications.Started)} for {e.TargetId} from {sender}");
            };
        }

        private static async Task CreateServerAsync(INotifierSerialiser jsonSerialiser)
        {
            var notifier = new Notifier<CalculationNotifications>(new NotifierSettings("https://localhost:7062/NotificationHub", NotifierPurpose.Transmitter, jsonSerialiser, false), _loggerFactory.CreateLogger("Server"));
            var notifications = await notifier.ConnectAsync();
            
            while (true)
            {
                _logger.LogInformation("Press Enter to send");
                Console.ReadLine();

                var sender = new FakeSender { SenderId = 5 };

                notifications.RaiseStarted(sender, new StartedEventArgs { TargetId = 555 });

                await Task.Delay(2000).ContinueWith(t =>
                {
                    notifications.RaiseFinished(sender, new FinishedEventArgs { TargetId = 555 });
                });
            }
        }
    }

    public class FakeSender
    {
        public int SenderId { get; set; }
    }
}