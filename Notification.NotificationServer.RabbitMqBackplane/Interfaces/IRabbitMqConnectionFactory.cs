using RabbitMQ.Client;

namespace Notification.NotificationServer.RabbitMqBackplane.Interfaces
{
    public interface IRabbitMqConnectionFactory
    {
        IAsyncConnectionFactory Connection { get; set; }
    }
}
