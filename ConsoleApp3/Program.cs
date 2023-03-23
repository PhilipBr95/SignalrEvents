using ClassLibrary1;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TPT.Notification.NotifierLibrary;

namespace ConsoleApp3
{
    internal class Program
    {
        private static ILoggerFactory _loggerFactory;
        private static ILogger<Program> _logger;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Information);
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
            });

            _logger = _loggerFactory.CreateLogger<Program>();

            var jsonSerialiser = new MySerialiser();

            if (args.Length > 0 && args[0] == "Client")
                await CreateClientAsync(jsonSerialiser);           
            else
                await CreateServerAsync(jsonSerialiser);
        }

        private static async Task CreateClientAsync(INotifierSerialiser jsonSerialiser)
        {
            var notifier = new Notifier<CalculationNotifications>(new NotifierSettings("https://localhost:5001/NotificationHub", NotifierPurpose.Receiver, jsonSerialiser, false), _loggerFactory.CreateLogger("Client"));
            var notifications = await notifier.ConnectAsync();

            notifications.Started += (object? sender, StartedEventArgs e) =>
            {
                _logger.LogInformation($"Received {nameof(notifications.Started)} for {e.TargetId} from {sender}");
            };

            notifications.Finished += (object? sender, FinishedEventArgs e) =>
            {
                _logger.LogInformation($"Received {nameof(notifications.Finished)} for {e.TargetId} from {sender}");
            };

            Console.ReadLine();
        }

        private static async Task CreateServerAsync(INotifierSerialiser jsonSerialiser)
        {
            var notifier = new Notifier<CalculationNotifications>(new NotifierSettings("https://localhost:5001/NotificationHub", NotifierPurpose.Transmitter, jsonSerialiser, false), _loggerFactory.CreateLogger("Client"));
            var notifications = await notifier.ConnectAsync();

            while (true)
            {                
                Console.ReadLine();
                
                notifications.RaiseStarted(new object(), new StartedEventArgs { TargetId = 9990 });

                await Task.Delay(5000).ContinueWith(t =>
                {
                    notifications.RaiseFinished(new object(), new FinishedEventArgs { TargetId = 9990 });
                });
            }
        }
    }

    public class MySerialiser : INotifierSerialiser
    {
        public object? Deserialise(string? json, Type returnType)
        {
            return JsonSerializer.Deserialize(json, returnType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public string Serialise(object? obj)
        {
            return JsonSerializer.Serialize(obj);
        }
    }
}