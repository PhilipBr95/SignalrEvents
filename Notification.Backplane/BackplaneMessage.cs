using System.Net.NetworkInformation;

namespace Notification.NotificationServer.Backplance
{
    public class BackplaneMessage
    {        
        public string Command { get; set; }
        public MessageData MessageData { get; set; }
        public string MessageId { get; set; }
    }

    public class MessageData
    {
        public object Sender { get; set; }
        public string ToConnectionId { get; set; }
        public string EventGroup { get; set; }
        public object Data { get; set; }
        public string EventName { get; set; }
    }
}