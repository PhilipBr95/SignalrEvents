using ClassLibrary1;
using ConsoleApp4.Models;
using Microsoft.Extensions.Logging;
using Notification.NotifierLibrary;

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

            if (args.Length > 0 && args[0] == "ClientServer")
            {
                await ListenForRequests();
            }
            else if (args.Length > 0 && args[0] == "Client")
                await CreateClientAsync("https://localhost:5001/NotificationHub");
            else
                await CreateServerAsync();
        }

        private static async Task ListenForRequests()
        {
            Console.WriteLine("Connecting...");
            Console.WriteLine("Press:");
            Console.WriteLine("1: Send to local SignalR");
            Console.WriteLine("2: Send to remote SignalR");
            Console.WriteLine("");

            var server = new Server("https://localhost:5001/NotificationHub", _loggerFactory.CreateLogger("Server"));
            _ = server.StartAsync();

            var client1 = new Client("https://localhost:5001/NotificationHub", 1, _loggerFactory.CreateLogger("Client1"));
            _ = client1.StartAsync();

            var client2 = new Client("http://localhost:8080/NotificationHub", 2, _loggerFactory.CreateLogger("Client2"));
            _ = client2.StartAsync();

            var notifier = new Notifier<CalculationNotification>(new NotifierSettings("https://localhost:5001/NotificationHub", NotifierPurpose.Receiver), _logger);

            while (true)
            {
                var key = Console.ReadKey();

                Console.WriteLine("-------------------------------------------------------------------------------------");

                switch(key.KeyChar)
                {
                    case '1':
                        await client1.SendAsync();
                        break;
                    case '2':
                        await client2.SendAsync();
                        break;
                }
            }
        }

        private static async Task<Request> CreateClient(string server, bool isPrivate)
        {
            var notifier = new Notifier<CalculationNotification>(new NotifierSettings(server, NotifierPurpose.Receiver), _logger);
            var notifications = await notifier.ConnectAsync();

            _requestCounter++;

            _logger.LogInformation($"Creating Notifier with isPrivate:{isPrivate}");

            NotificationSettings? settings = isPrivate ? new NotificationSettings { ConnectionId = notifier.ConnectionId } : null;
            var request = new Request { RequestId = _requestCounter, NotificationSettings = settings };
            request.Events = notifications;

            return request;
        }

        private static async Task CreateClientAsync(string server, char? activationKey = null)
        {
            var logger = _loggerFactory.CreateLogger($"Client {activationKey}");
            logger.LogInformation($"Client running");

            while (true)
            {
                var key = Console.ReadKey();
                if (activationKey == null || key.KeyChar == activationKey)
                {
                    var request = await CreateRequestAsync(server, logger, activationKey == '3');
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

        private async static Task<Request> CreateRequestAsync(string server, ILogger logger, bool isPrivate)
        {            
            var notifier = new Notifier<CalculationNotification>(new NotifierSettings(server, NotifierPurpose.Receiver), logger);
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
}