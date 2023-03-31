namespace Notification.NotificationServer.Backplane.Models
{
    public class BackplaneMessage
    {
        public string MessageId { get; set; }
        public string Command { get; set; }
        public MessageData MessageData { get; set; }     
    }
}