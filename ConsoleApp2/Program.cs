using ClassLibrary1;
using Microsoft.Extensions.Logging;
using SignalClassLibrary;

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

            await CreateCalc_ClientAsync("CalcClient 1");
            await CreateTree_ClientAsync("TreeClient 1");
            await CreateServerAsync();          
        }

        private static async Task CreateCalc_ClientAsync(string loggerName) 
        {
            var notifier = new Notifier<CalculationNotifications>(new NotifierSettings { Url = "https://localhost:7062/ChatHub" }, _loggerFactory.CreateLogger(loggerName));
            var notifications = await notifier.ConnectAsync(NotifierPurposes.Receiver);

            notifications.Started += (object? sender, StartedEventArgs e) =>
            {
                _logger.LogInformation($"{loggerName} Received {e.TargetId} from {sender}");
            };
        }

        private static async Task CreateTree_ClientAsync(string loggerName)
        {
            var notifier = new Notifier<TreeNotifications>(new NotifierSettings { Url = "https://localhost:7062/ChatHub" }, _loggerFactory.CreateLogger(loggerName));
            var notifications = await notifier.ConnectAsync(NotifierPurposes.Receiver);

            notifications.Started += (object? sender, StartedEventArgs e) =>
            {
                _logger.LogInformation($"{loggerName} Received {e.TargetId} from {sender}");
            };
        }

        private static async Task CreateServerAsync()
        {
            var notifier = new Notifier<CalculationNotifications>(new NotifierSettings { Url = "https://localhost:7062/ChatHub" }, _loggerFactory.CreateLogger("Server"));
            var notifications = await notifier.ConnectAsync(NotifierPurposes.Transmitter);
            
            while (true)
            {
                _logger.LogInformation("Press Enter to send");
                Console.ReadLine();

                notifications.RaiseStarted("Server 1", new StartedEventArgs { TargetId = 555 });
            }
        }
    }
}