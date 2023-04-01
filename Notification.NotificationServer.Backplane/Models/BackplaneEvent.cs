namespace Notification.NotificationServer.Backplane.Models
{
    public class BackplaneEvent : EventArgs
    {
        public BackplaneMessage Message { get; private set; }

        public BackplaneEvent(BackplaneMessage message)
        {
            Message = message;
        }
    }
}