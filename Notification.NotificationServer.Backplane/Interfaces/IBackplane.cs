using Microsoft.AspNetCore.SignalR;
using Notification.NotificationServer.Backplane.Models;

namespace Notification.NotificationServer.Backplane.Interfaces
{
    public interface IBackplane<THub, TMessage> where THub : Hub
                                                where TMessage : class
    {
        void Send(string connectionId, string command, TMessage message);
        void AddReceived(Action<object, IHubContext<THub>, BackplaneEvent<TMessage>> value);
        string ConsumerTag { get; }
    }
}