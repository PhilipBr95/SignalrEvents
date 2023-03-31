namespace Notification.NotificationServer.Backplance
{
    public interface IBackplane
    {
        void Send(string command, MessageData messageCata);

        event EventHandler<BackplaneEvent> Received;
    }

    public class BackplaneEvent : EventArgs
    {
        public BackplaneMessage Message { get; private set; }

        public BackplaneEvent(BackplaneMessage message)
        {
            Message = message;
        }
    }
}