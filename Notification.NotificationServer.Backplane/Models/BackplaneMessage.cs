namespace Notification.NotificationServer.Backplane.Models
{
    public class BackplaneMessage
    {
        public string ConnectionId { get; set; }
        public string MessageId { get; set; }
        public string Command { get; set; }
        public MessageData MessageData { get; set; }
    }
}