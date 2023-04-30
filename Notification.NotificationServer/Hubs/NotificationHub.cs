using Microsoft.AspNetCore.SignalR;
using Notification.NotificationServer.Backplane.Interfaces;
using Notification.NotificationServer.Backplane.Models;
using Notification.NotifierLibrary;
using System;

namespace Notification.NotificationServer
{
    public class NotificationHub : Hub
    {
        private readonly IBackplane<NotificationHub, NotifierEventArgs> _backplane;
        private readonly ILogger<NotificationHub> _logger;
        public const string RAISEEVENT_NAME = "RaiseEvent";

        public NotificationHub(IBackplane<NotificationHub, NotifierEventArgs> backplane, ILogger<NotificationHub> logger) 
        {
            _backplane = backplane;
            _logger = logger;

            _backplane.AddReceived((sender, hubContext, e) => OnBackPlaneReceived(sender, hubContext, e));
        }

        private void OnBackPlaneReceived(object? sender, IHubContext<NotificationHub> hubContext, BackplaneEvent<NotifierEventArgs> e) 
        {
            try
            {
                _logger?.LogInformation($"{nameof(OnBackPlaneReceived)} - Message {e.Message.MessageId}");

                _ = RaiseEventFromBackplaneAsync(hubContext, e.Message.EventArgs);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Message {e.Message.MessageId}");
                throw;
            }
        }

        public override Task OnConnectedAsync()
        {
            _logger?.LogInformation($"{Context.ConnectionId} Connected");

            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var msg = $"{Context.ConnectionId} disconnected {(exception != null ? $"with {exception}" : string.Empty)}";
            _logger?.LogInformation(msg);

            return base.OnDisconnectedAsync(exception);
        }

        public async Task RaiseEvent(NotifierEventArgs notifierEventArgs)
        {
            _logger?.LogInformation($"Received event {notifierEventArgs.EventGroup}=>{notifierEventArgs.EventName} - {notifierEventArgs.Json} from {Context.ConnectionId} in {notifierEventArgs.EventGroup}");

            //Send it to everybody in the group, except the sender
            await Clients.GroupExcept(notifierEventArgs.EventGroup, Context.ConnectionId)
                            .SendAsync(RAISEEVENT_NAME, notifierEventArgs);

            _backplane.Send(Context.ConnectionId, nameof(RaiseEvent), notifierEventArgs);
        }

        private async Task RaiseEventFromBackplaneAsync(IHubContext<NotificationHub> hubContext, NotifierEventArgs notifierEventArgs)
        {
            _logger?.LogInformation($"Received event {notifierEventArgs.EventGroup}=>{notifierEventArgs.EventName} - {notifierEventArgs.Json} from backplane");

            //Send it to everybody in the group.  The sender is on the other server
            await hubContext.Clients.Group(notifierEventArgs.EventGroup)
                                    .SendAsync(RAISEEVENT_NAME, notifierEventArgs);
        }

        public async Task JoinGroup(string eventGroup)
        {
            _logger?.LogInformation($"{Context.ConnectionId} joining {eventGroup}");

            await Groups.AddToGroupAsync(Context.ConnectionId, eventGroup);
        }
    }
}
