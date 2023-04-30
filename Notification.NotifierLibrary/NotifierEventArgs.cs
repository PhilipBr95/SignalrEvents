namespace Notification.NotifierLibrary
{
    public class NotifierEventArgs
    {
        public string EventGroup { get; set; }
        public string Sender { get; set; }
        public string EventName { get; set; }
        public string Json { get; set; }
    }

}