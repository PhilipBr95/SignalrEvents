using Notification.NotificationServer.Backplane.Models;

namespace Notification.NotificationServer.Backplane.Interfaces
{
    public interface IBackplane
    {
        void Send(string command, MessageData messageCata);

        event EventHandler<BackplaneEvent> Received;
    }
}