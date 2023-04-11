using Microsoft.AspNetCore.SignalR;
using Notification.NotificationServer.Backplane.Interfaces;
using Notification.NotificationServer.Backplane.Models;

namespace Notification.NotificationServer
{
    public class NotificationHub : Hub
    {
        private readonly IBackplane<NotificationHub> _backplane;
        private readonly ILogger<NotificationHub> _logger;
        public const string RAISEEVENT_NAME = "RaiseEvent";

        public NotificationHub(IBackplane<NotificationHub> backplane, ILogger<NotificationHub> logger) 
        {
            _backplane = backplane;
            _logger = logger;

            _backplane.AddReceived((sender, hubContext, e) => OnBackPlaneReceived(sender, hubContext, e));
        }

        private void OnBackPlaneReceived(object? sender, IHubContext<NotificationHub> hubContext, BackplaneEvent e) 
        {
            try
            {
                _logger?.LogInformation($"{nameof(OnBackPlaneReceived)} - Message {e.Message.MessageId}");

                var messageData = e.Message.MessageData;

                switch (e.Message.Command)
                {
                    case nameof(RaiseGroupEvent):
                        _ = RaiseGroupEventFromBackplaneAsync(messageData.Sender, hubContext, messageData.EventGroup, messageData.EventName, messageData.Data);
                        break;
                    case nameof(RaiseClientGroupEvent):
                        _ = RaiseClientGroupEventFromBackplaneAsync(messageData.Sender, hubContext, messageData.ToConnectionId, messageData.EventGroup, messageData.EventName, messageData.Data);
                        break;
                    default: throw new ArgumentException($"unknown Command {e.Message.Command}");
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Message {e.Message.MessageId}");
                throw;
            }
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            var msg = $"{Context.ConnectionId} disconnected {(exception != null ? $"with {exception}" : string.Empty)}";
            _logger?.LogInformation(msg);

            return base.OnDisconnectedAsync(exception);
        }

        public async Task RaiseGroupEvent(object sender, string eventGroup, string eventName, object data)
        {
            _logger?.LogInformation($"Received event {eventGroup}=>{eventName} - {data} from {Context.ConnectionId}");

            //Send it to everybody in the group, except the sender
            _ = Clients.GroupExcept(eventGroup, Context.ConnectionId)
                       .SendAsync(RAISEEVENT_NAME, sender, eventGroup, eventName, data);

            _backplane.Send(Context.ConnectionId, nameof(RaiseGroupEvent), new MessageData { Sender = sender, EventGroup = eventGroup, EventName = eventName, Data = data });
        }

        private async Task RaiseGroupEventFromBackplaneAsync(object sender, IHubContext<NotificationHub> hubContext, string eventGroup, string eventName, object data)
        {
            _logger?.LogInformation($"Received event {eventGroup}=>{eventName} - {data} from backplane");
                
            //Send it to everybody in the group, except the sender
            await hubContext.Clients.Group(eventGroup)
                                    .SendAsync(RAISEEVENT_NAME, sender, eventGroup, eventName, data);            
        }  

        public async Task RaiseClientGroupEvent(object sender, string toConnectionId, string eventGroup, string eventName, object data)
        {
            _logger?.LogInformation($"Received event {eventGroup}=>{eventName} - {data} from {Context.ConnectionId} for {toConnectionId}");

            //Send it to everybody in the group, except the sender
            _ = Clients.Client(toConnectionId)
                       .SendAsync(RAISEEVENT_NAME, sender, eventGroup, eventName, data);

            _backplane.Send(Context.ConnectionId, nameof(RaiseClientGroupEvent), new MessageData { Sender = sender, ToConnectionId = toConnectionId, EventGroup = eventGroup, EventName = eventName, Data = data });
        }

        public async Task RaiseClientGroupEventFromBackplaneAsync(object sender, IHubContext<NotificationHub> hubContext, string toConnectionId, string eventGroup, string eventName, object data)
        {
            _logger?.LogInformation($"Received event {eventGroup}=>{eventName} - {data} from backplane for {toConnectionId}");

            //Send it to everybody in the group, except the sender
            await hubContext.Clients.Client(toConnectionId)
                                    .SendAsync(RAISEEVENT_NAME, sender, eventGroup, eventName, data);
        }

        public async Task JoinGroup(string eventGroup)
        {
            _logger?.LogInformation($"{Context.ConnectionId} joining {eventGroup}");

            await Groups.AddToGroupAsync(Context.ConnectionId, eventGroup);
        }
    }
}
