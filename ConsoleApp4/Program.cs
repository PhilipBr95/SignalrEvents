using ClassLibrary1;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;
using TPT.Notification.NotifierLibrary;
using static ConsoleApp4.RequestNotification;

namespace ConsoleApp4
{
    internal class Program
    {
        private static ILoggerFactory _loggerFactory;
        private static ILogger<Program> _logger;
        private static Request _request;
        private static int _requestCounter = 0;

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

            if (args.Length > 0 && args[0] == "ClientServer")
            {
                _ = Task.Run(() => CreateServerAsync());

                _ = Task.Run(() => CreateClientAsync('1'));
                await Task.Run(() => CreateClientAsync('2'));
            }
            else if (args.Length > 0 && args[0] == "Client")
                await CreateClientAsync();
            else
                await CreateServerAsync();
        }

        private static async Task CreateClientAsync(char? activationKey = null)
        {
            var logger = _loggerFactory.CreateLogger($"Client {activationKey}");
            logger.LogInformation($"Client running");

            while (true)
            {
                var key = Console.ReadKey();
                if (activationKey == null || key.KeyChar == activationKey)
                {
                    var request = await CreateRequestAsync(logger, activationKey == '1');
                    logger.LogInformation($"Sent 'RequestId {request.RequestId}'");

                    request.Events.Started += (object? sender, StartedEventArgs e) =>
                    {
                        logger.LogInformation($"Received {nameof(request.Events.Started)} for {e.RequestId} from {sender}");
                    };

                    request.Events.Finished += (object? sender, FinishedEventArgs e) =>
                    {
                        logger.LogInformation($"Received {nameof(request.Events.Finished)} for {e.RequestId} from {sender}");
                        logger.LogInformation($"Finished {e.RequestId}...");
                    };
                }
            }
        }

        private async static Task<Request> CreateRequestAsync(ILogger logger, bool isPrivate)
        {            
            var notifier = new Notifier<CalculationNotification>(new NotifierSettings("https://localhost:5001/NotificationHub", NotifierPurpose.Receiver), logger);
            var notifications = await notifier.ConnectAsync();

            _requestCounter++;

            logger.LogInformation($"Creating Notifier with isPrivate:{isPrivate}");

            NotificationSettings? settings = isPrivate ? new NotificationSettings { ConnectionId = notifier.ConnectionId } : null;
            var request = new Request { RequestId = _requestCounter, NotificationSettings = settings };
            request.Events = notifications;

            //Ickle hack as Client+Server are both in memory
            _request = request;

            return request;
        }

        private static async Task CreateServerAsync()
        {
            //var reciever = new Notifier<RequestNotification>(new NotifierSettings("https://localhost:5001/NotificationHub", NotifierPurpose.Receiver, false), _loggerFactory.CreateLogger("Client"));
            //var recieverNotifications = await reciever.ConnectAsync();

            //recieverNotifications.ProcessingRequired += (object? sender, ProcessingRequiredEventArgs e) =>
            //{
            //    _logger.LogInformation($"ProcessingRequired for {e.} from {sender}");
            //};

            var logger = _loggerFactory.CreateLogger("Server");
            logger.LogInformation($"Server running");

            while (true)
            {
                //A little fakery
                if (_request == null)
                {
                    Thread.Sleep(500);
                    continue;

                }

                logger.LogInformation($"Received 'RequestId {_request.RequestId}'");

                await using (var notifier = new Notifier<CalculationNotification>(new NotifierSettings("https://localhost:5001/NotificationHub", NotifierPurpose.Transmitter, _request.NotificationSettings?.ConnectionId), logger))
                {
                    var notifications = await notifier.ConnectAsync();

                    notifications.RaiseStarted(new object(), new StartedEventArgs { RequestId = _request.RequestId });

                    await Task.Delay(5000).ContinueWith(t =>
                    {
                        notifications.RaiseFinished(new object(), new FinishedEventArgs { RequestId = _request.RequestId });
                    });
                }

                //Finished the work :-)
                logger.LogInformation($"Finished {_request.RequestId}...");
                _request = null;
            }
        }

    }

    public class RequestNotification
    {
        public event EventHandler<ProcessingRequiredEventArgs> ProcessingRequired;

        public void RaiseProcessingRequired(object sender, ProcessingRequiredEventArgs e)
        {
            ProcessingRequired?.Invoke(sender, e);
        }

        public class ProcessingRequiredEventArgs : EventArgs { }
    }

    internal class Request
    {
        public int RequestId { get; set; }
        public NotificationSettings NotificationSettings { get; set; }
        public CalculationNotification Events { get; set; }
    }

    public class NotificationSettings
    {
        public string ConnectionId { get; set; }
    }
}