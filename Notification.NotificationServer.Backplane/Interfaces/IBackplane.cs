using Microsoft.AspNetCore.SignalR;
using Notification.NotificationServer.Backplane.Models;

namespace Notification.NotificationServer.Backplane.Interfaces
{
    public interface IBackplane<THub> where THub : Hub
    {
        void Send(string connectionId, string command, MessageData messageCata);
        void AddReceived(Action<object, IHubContext<THub>, BackplaneEvent> value);
    }
}