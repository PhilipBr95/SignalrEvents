namespace Notification.NotifierLibrary
{
    public class NotifierSettings
    {
        public string Url { get; }
        public NotifierPurpose Purpose { get; }
        public INotifierSerialiser Serialiser { get; }

        public NotifierSettings(string url, NotifierPurpose purpose, INotifierSerialiser serialiser) 
        {
            Url = url;
            Purpose = purpose;
            Serialiser = serialiser;
        }
    }
}