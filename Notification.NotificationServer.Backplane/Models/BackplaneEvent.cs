namespace Notification.NotificationServer.Backplane.Models
{
    public class BackplaneEvent<T> : EventArgs where T : class
    {
        public BackplaneMessage<T> Message { get; private set; }

        public BackplaneEvent(BackplaneMessage<T> message)
        {
            Message = message;
        }
    }
}