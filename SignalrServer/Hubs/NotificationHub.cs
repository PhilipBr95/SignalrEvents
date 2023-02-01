using Microsoft.AspNetCore.SignalR;
using SignalClassLibrary;

namespace SignalrServer.Hubs
{
    public class NotificationHub : Hub
    {
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }

        public async Task Send(string ClientId, string message)
        {
            await Clients.All.SendAsync(nameof(Actions.SendToClient), ClientId, message);
        }

        public async Task SendToAll(string message)
        {
            await Clients.All.SendAsync(nameof(Actions.SendToAll), message);
        }
    }
}
