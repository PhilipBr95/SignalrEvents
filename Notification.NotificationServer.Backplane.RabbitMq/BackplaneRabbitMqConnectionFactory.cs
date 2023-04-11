using Notification.NotificationServer.Backplane.RabbitMq.Interfaces;
using RabbitMQ.Client;

namespace Notification.NotificationServer.Backplane.RabbitMq
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
