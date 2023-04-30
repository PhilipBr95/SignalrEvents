using ClassLibrary1;
using ConsoleApp4.Models;
using Microsoft.Extensions.Logging;
using Notification.NotifierLibrary;

namespace ConsoleApp4
{
    public class Client
    {
        private readonly string _server;
        private readonly int _requestId;
        private readonly ILogger _logger;
        private Request _request;

        public Client(string server, int requestId, ILogger logger)
        {
            _server = server;
            _requestId = requestId;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            var notifier = new Notifier<CalculationNotification>(new NotifierSettings(_server, NotifierPurpose.Receiver, null), _logger);
            var notifications = await notifier.ConnectAsync();

            var request = new Request { RequestId = _requestId};
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
}