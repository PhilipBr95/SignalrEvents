using ClassLibrary1;
using Microsoft.Extensions.Logging;
using Notification.NotifierLibrary;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp_462
{
    internal class Program
    {
        private static ILoggerFactory _loggerFactory;
        private static ILogger<Program> _logger;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Waiting for the SignalR to spin up...");
            Thread.Sleep(5000);

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

            var notifier = new Notifier<CalculationNotification>(new NotifierSettings("http://localhost:8080/NotificationHub", NotifierPurpose.Receiver, null), _logger);
            var notifications = await notifier.ConnectAsync();

            notifications.RaiseStarted(null, new StartedEventArgs{ RequestId = 555 });

        }
    }

}