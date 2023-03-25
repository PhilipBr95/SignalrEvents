using System.Text.Json;

namespace TPT.Notification.NotifierLibrary
{
    internal class DefaultSerialiser : INotifierSerialiser
    {
        public object? Deserialise(string? json, Type returnType)
        {
            return JsonSerializer.Deserialize(json, returnType, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public string Serialise(object? obj)
        {
            return JsonSerializer.Serialize(obj);
        }
    }
}