using RabbitMQ.Client;

namespace Notification.NotificationServer.Backplane.RabbitMq.Interfaces
{
    public interface IRabbitMqConnectionFactory
    {
        IAsyncConnectionFactory Connection { get; set; }
    }
}
