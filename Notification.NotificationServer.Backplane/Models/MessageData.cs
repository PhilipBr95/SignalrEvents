namespace Notification.NotificationServer.Backplane.Models
{
    public class MessageData
    {
        public object Sender { get; set; }
        public string ToConnectionId { get; set; }
        public string EventGroup { get; set; }
        public object Data { get; set; }
        public string EventName { get; set; }
    }
}