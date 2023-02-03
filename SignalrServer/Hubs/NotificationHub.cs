using Microsoft.AspNetCore.SignalR;

namespace SignalrServer.Hubs
{
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

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
            _logger.LogInformation($"{Context.ConnectionId} disconnected with {exception}");
            return base.OnDisconnectedAsync(exception);
        }

        public async Task RaiseEvent(string eventGroup, string eventName, string json)
        {            
            _logger?.LogInformation($"Received event {eventGroup}=>{eventName} - {json} from {Context.ConnectionId}");

            //Send it to everybody in theis group, except the sender
            await Clients.GroupExcept(eventGroup, Context.ConnectionId)
                         .SendAsync(nameof(RaiseEvent), eventGroup, eventName, json);
        }
        public async Task JoinGroup(string eventGroup)
        {
            _logger?.LogInformation($"{Context.ConnectionId} joining {eventGroup}");

            await Groups.AddToGroupAsync(Context.ConnectionId, eventGroup);
        }
    }
}
