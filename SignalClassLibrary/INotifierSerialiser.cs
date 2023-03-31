namespace Notification.NotifierLibrary
{
    /// <summary>
    /// To handle the switchover from Newtonsoft to Text.Json
    /// </summary>
    public interface INotifierSerialiser
    {
        string Serialise(object? obj);
        object? Deserialise(string? json, Type returnType);
    }
}