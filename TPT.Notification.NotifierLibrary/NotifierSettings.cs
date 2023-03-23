namespace TPT.Notification.NotifierLibrary
{
    public class NotifierSettings
    {
        public string Url { get; }
        public NotifierPurpose Purpose { get; }
        public INotifierSerialiser Serialiser { get; }
        public bool IsPrivate { get; }

        public NotifierSettings(string url, NotifierPurpose purpose, INotifierSerialiser serialiser, bool isPrivate) 
        {
            Url = url;
            Purpose = purpose;
            Serialiser = serialiser;
            IsPrivate = isPrivate;
        }
    }
}