using Microsoft.AspNetCore.SignalR;
using Notification.NotificationServer.Backplane.Interfaces;
using Notification.NotificationServer.Backplane.Models;

namespace Notification.NotificationServer
{
    public class NotificationHub : Hub
    {
        private readonly IBackplane _backplane;
        private readonly ILogger<NotificationHub> _logger;
        public const string RAISEEVENT_NAME = "RaiseEvent";

        public NotificationHub(IBackplane backplane, ILogger<NotificationHub> logger)
        {
            _backplane = backplane;
            _logger = logger;

            _backplane.Received += OnBackPlaneReceived;
        }

        private void OnBackPlaneReceived(object? sender, BackplaneEvent e)
        { 
            _logger?.LogInformation($"{nameof(OnBackPlaneReceived)} - {e.Message.MessageId}");

            var messageData = e.Message.MessageData;

            switch (e.Message.Command)
            {
                case nameof(RaiseGroupEvent):
                    _ = RaiseGroupEvent(messageData.Sender, messageData.EventGroup, messageData.EventName, messageData.Data);
                    break;
                case nameof(RaiseClientGroupEvent):
                    _ = RaiseClientGroupEvent(messageData.Sender, messageData.ToConnectionId, messageData.EventGroup, messageData.EventName, messageData.Data);
                    break;
                default: throw new ArgumentException($"unknown Command {e.Message.Command}");
            }
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger?.LogInformation($"{Context.ConnectionId} disconnected with {exception}");
            return base.OnDisconnectedAsync(exception);
        }

        public async Task RaiseGroupEvent(object sender, string eventGroup, string eventName, object data)
        {            
            _logger?.LogInformation($"Received event {eventGroup}=>{eventName} - {data} from {Context.ConnectionId}");
            
            //Send it to everybody in the group, except the sender
            await Clients.GroupExcept(eventGroup, Context.ConnectionId)
                         .SendAsync(RAISEEVENT_NAME, sender, eventGroup, eventName, data);

            _backplane.Send(nameof(RaiseGroupEvent), new MessageData { Sender = sender, EventGroup = eventGroup, EventName = eventName, Data = data });
        }

        public async Task RaiseClientGroupEvent(object sender, string toConnectionId, string eventGroup, string eventName, object data)
        {
            _logger?.LogInformation($"Received event {eventGroup}=>{eventName} - {data} from {Context.ConnectionId} for {toConnectionId}");

            //Send it to everybody in the group, except the sender
            await Clients.Client(toConnectionId)
                         .SendAsync(RAISEEVENT_NAME, sender, eventGroup, eventName, data);

            _backplane.Send(nameof(RaiseClientGroupEvent), new MessageData { Sender = sender, ToConnectionId = toConnectionId, EventGroup = eventGroup, EventName = eventName, Data = data });
        }

        public async Task JoinGroup(string eventGroup)
        {
            _logger?.LogInformation($"{Context.ConnectionId} joining {eventGroup}");

            await Groups.AddToGroupAsync(Context.ConnectionId, eventGroup);
        }
    }
}
