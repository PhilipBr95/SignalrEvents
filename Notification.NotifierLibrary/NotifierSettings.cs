namespace Notification.NotifierLibrary
{
    public class NotifierSettings
    {
        public string Url { get; }
        public NotifierPurpose Purpose { get; }
        public INotifierSerialiser Serialiser { get; }

        /// <summary>
        /// Notifier settings - Specify connectionId for private notifications
        /// </summary>
        /// <param name="url">Signalr url</param>
        /// <param name="purpose">It's best to pick one or the other, though you can pick both if you know what you're doing</param>
        /// <param name="serialiser">Alternative serialiser</param>
        public NotifierSettings(string url, NotifierPurpose purpose, string? connectionId = null, INotifierSerialiser? serialiser = null) 
        {
            Url = url;
            Purpose = purpose;
            Serialiser = serialiser ?? new DefaultSerialiser();
        }
    }
}