using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace TPT.Notification.NotificationServer
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        public const string RAISEEVENT_NAME = "RaiseEvent";

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
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
        }

        public async Task RaiseClientGroupEvent(object sender, string toConnectionId, string eventGroup, string eventName, object data)
        {
            _logger?.LogInformation($"Received event {eventGroup}=>{eventName} - {data} from {Context.ConnectionId} for {toConnectionId}");

            //Send it to everybody in the group, except the sender
            await Clients.Client(toConnectionId)
                         .SendAsync(RAISEEVENT_NAME, sender, eventGroup, eventName, data);
        }

        public async Task JoinGroup(string eventGroup)
        {
            _logger?.LogInformation($"{Context.ConnectionId} joining {eventGroup}");

            await Groups.AddToGroupAsync(Context.ConnectionId, eventGroup);
        }
    }
}
