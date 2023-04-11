using ClassLibrary1;
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
                await ListenForRequests();
            }
            else if (args.Length > 0 && args[0] == "Client")
                await CreateClientAsync("https://localhost:5001/NotificationHub");
            else
                await CreateServerAsync();
        }

        private static async Task ListenForRequests()
        {
            var server = new Server("https://localhost:5001/NotificationHub", _loggerFactory.CreateLogger("Server"));
            _ = server.StartAsync();

            var client1 = new Client("https://localhost:5001/NotificationHub", 1, false, _loggerFactory.CreateLogger("Client1"));
            _ = client1.StartAsync();

            var client2 = new Client("http://localhost:8080/NotificationHub", 2, false, _loggerFactory.CreateLogger("Client2"));
            _ = client2.StartAsync();

            var privateClient3 = new Client("http://localhost:8080/NotificationHub", 3, true, _loggerFactory.CreateLogger("PrivateClient3"));
            _ = privateClient3.StartAsync();

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
                    case '3':
                        await privateClient3.SendAsync();
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

    public class Client
    {
        private readonly string _server;
        private readonly int _requestId;
        private readonly bool _isPrivate;
        private readonly ILogger _logger;
        private Request _request;

        public Client(string server, int requestId, bool isPrivate, ILogger logger)
        {
            _server = server;
            _requestId = requestId;
            _isPrivate = isPrivate;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            var notifier = new Notifier<CalculationNotification>(new NotifierSettings(_server, NotifierPurpose.Receiver), _logger);
            var notifications = await notifier.ConnectAsync();

            _logger.LogInformation($"Creating Notifier with isPrivate:{_isPrivate}");

            NotificationSettings? settings = _isPrivate ? new NotificationSettings { ConnectionId = notifier.ConnectionId } : null;
            var request = new Request { RequestId = _requestId, NotificationSettings = settings };
            request.Events = notifications;

            request.Events.Started += (object? sender, StartedEventArgs e) =>
            {
                _logger.LogInformation($"Received {nameof(request.Events.Started)} for {e.RequestId} from {sender}");
            };

            request.Events.Finished += (object? sender, FinishedEventArgs e) =>
            {
                _logger.LogInformation($"Received {nameof(request.Events.Finished)} for {e.RequestId} from {sender}");
                _logger.LogInformation($"Finished {e.RequestId}...");
            };

            _request = request;
        }

        internal async Task SendAsync()
        {
            await using (var requestNotifier = new Notifier<RequestNotification>(new NotifierSettings(_server, NotifierPurpose.Transmitter, null), _logger))
            {
                var notifier = await requestNotifier.ConnectAsync();
                notifier.RaiseProcessingRequired(this, new RequestNotification.ProcessingRequiredEventArgs {  RequestId = _requestId });
            }
        }
    }

    public class Server 
    {
        private readonly string _server;
        private readonly ILogger _logger;
        private Notifier<CalculationNotification> _replyNotifier;
        private Notifier<RequestNotification> _receiveNotifier;

        public Server(string server, ILogger logger)
        {
            _server = server;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            _logger.LogInformation($"Server running");

            _replyNotifier = new Notifier<CalculationNotification>(new NotifierSettings(_server, NotifierPurpose.Transmitter, null), _logger);
            _receiveNotifier = new Notifier<RequestNotification>(new NotifierSettings(_server, NotifierPurpose.Receiver, null), _logger);

            var replyNotifications = await _replyNotifier.ConnectAsync();
            var receiveNotifications = await _receiveNotifier.ConnectAsync();

            receiveNotifications.ProcessingRequired += (sender, request) =>
            {
                _logger.LogInformation($"Received 'RequestId {request.RequestId}'");

                replyNotifications.RaiseStarted(new object(), new StartedEventArgs { RequestId = request.RequestId });

                Task.Delay(5000).ContinueWith(t =>
                {
                    replyNotifications.RaiseFinished(new object(), new FinishedEventArgs { RequestId = request.RequestId });
                });
            };
        }
    }

    public class RequestNotification
    {
        public event EventHandler<ProcessingRequiredEventArgs> ProcessingRequired;

        public void RaiseProcessingRequired(object sender, ProcessingRequiredEventArgs e)
        {
            ProcessingRequired?.Invoke(sender, e);
        }

        public class ProcessingRequiredEventArgs : EventArgs 
        {
            public int RequestId { get; set; }
        }
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