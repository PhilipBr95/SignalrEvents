namespace Notification.NotificationServer.Backplane.Models
{
    public class BackplaneMessage<T> where T : class
    {
        public string ConnectionId { get; set; }
        public string MessageId { get; set; }
        public string Command { get; set; }
        public T EventArgs { get; set; }
    }
}