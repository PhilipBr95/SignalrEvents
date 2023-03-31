using Notification.NotificationServer.RabbitMqBackplane.Interfaces;
using RabbitMQ.Client;

namespace Notification.NotificationServer.RabbitMqBackplane
{
    public class BackplaneRabbitMqConnectionFactory : IRabbitMqConnectionFactory
    {
        public IAsyncConnectionFactory Connection { get; set; }

        public BackplaneRabbitMqConnectionFactory(IAsyncConnectionFactory connectionFactory)
        {
            Connection = connectionFactory;
        }
    }
}
