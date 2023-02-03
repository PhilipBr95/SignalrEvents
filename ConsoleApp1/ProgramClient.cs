using ClassLibrary1;
using Microsoft.Extensions.Logging;
using SignalClassLibrary;

namespace ConsoleAppClient
{
    public class ProgramClient
    {
        private static Notifier<CalculationNotifications> _notifier;
        private static ILogger<ProgramClient> _logger;

        static async Task Main(string[] args)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddSimpleConsole();
            });

            _logger = loggerFactory.CreateLogger<ProgramClient>();

            _logger.LogInformation("Hello, Client 1.  Press Enter to connect");
            Console.ReadLine();

            _notifier = new Notifier<CalculationNotifications>(new NotifierSettings { Url = "https://localhost:7061/ChatHub" }, loggerFactory.CreateLogger<Notifier<CalculationNotifications>>());
            
            var notifications = await _notifier.ConnectAsync();
            notifications.Started += Notifications_Started1; ;
            
            while (true)
            {
                _logger.LogInformation("Client 1 Running - Press something to send");
                Console.ReadLine();

                notifications.RaiseFinished(null, new FinishedEventArgs { TargetId = 5554 });
            }
        }

        private static void Notifications_Started1(object? sender, StartedEventArgs e)
        {
            _logger.LogInformation("Started");
        }

    }

}