using ClassLibrary1;
using ConsoleApp4.Models;
using Microsoft.Extensions.Logging;
using Notification.NotifierLibrary;

namespace ConsoleApp4
{
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

                replyNotifications.RaiseStarted(this, new StartedEventArgs { RequestId = request.RequestId });

                Task.Delay(5000).ContinueWith(t =>
                {
                    replyNotifications.RaiseFinished(this, new FinishedEventArgs { RequestId = request.RequestId });
                });
            };
        }
    }
}