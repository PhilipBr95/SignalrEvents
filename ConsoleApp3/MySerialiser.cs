using System.Text.Json;
using TPT.Notification.NotifierLibrary;

namespace ConsoleApp3
{
    public class MySerialiser : INotifierSerialiser
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