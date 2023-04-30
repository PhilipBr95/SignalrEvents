using ClassLibrary1;
using Microsoft.Extensions.Logging;
using Notification.NotifierLibrary;

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
                builder.SetMinimumLevel(LogLevel.Debug);
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
            var notifier = new Notifier<CalculationNotification>(new NotifierSettings("https://localhost:5001/NotificationHub", NotifierPurpose.Receiver, null, jsonSerialiser), _loggerFactory.CreateLogger("Client"));
            var notifications = await notifier.ConnectAsync();

            notifications.Started += (object? sender, StartedEventArgs e) =>
            {
                _logger.LogInformation($"Received {nameof(notifications.Started)} for {e.RequestId} from {sender}");
            };

            notifications.Finished += (object? sender, FinishedEventArgs e) =>
            {
                _logger.LogInformation($"Received {nameof(notifications.Finished)} for {e.RequestId} from {sender}");
            };

            Console.ReadLine();
        }

        private static async Task CreateServerAsync(INotifierSerialiser jsonSerialiser)
        {
            var notifier = new Notifier<CalculationNotification>(new NotifierSettings("https://localhost:5001/NotificationHub", NotifierPurpose.Transmitter, null, jsonSerialiser), _loggerFactory.CreateLogger("Client"));
            var notifications = await notifier.ConnectAsync();

            while (true)
            {                
                Console.ReadLine();
                
                notifications.RaiseStarted(new object(), new StartedEventArgs { RequestId = 9990 });

                await Task.Delay(5000).ContinueWith(t =>
                {
                    notifications.RaiseFinished(new object(), new FinishedEventArgs { RequestId = 9990 });
                });
            }
        }
    }
}