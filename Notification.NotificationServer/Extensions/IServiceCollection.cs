using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Runtime.CompilerServices;
using System.Text;
using Notification.NotificationServer.Backplance;

namespace Notification.NotificationServer.Extensions
{
    public interface IBackplaneRabbitMqConnectionFactory
    {
        IAsyncConnectionFactory Connection { get; set; }
    }

    public class BackplaneRabbitMqConnectionFactory : IBackplaneRabbitMqConnectionFactory
    {
        public IAsyncConnectionFactory Connection { get; set; }

        public BackplaneRabbitMqConnectionFactory(IAsyncConnectionFactory connectionFactory)
        {
            Connection = connectionFactory;
        }
    }

    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMqBackPlane(this IServiceCollection serviceCollection, Action<RabbitMqBackPlaneOptions> options)
        {
            var sp = serviceCollection.BuildServiceProvider();            
            var logger = sp.GetRequiredService<ILogger<IServiceCollection>>();

            serviceCollection.AddOptions();

            //Get from param
            var optionsTodo = new RabbitMqOptions();

            //Todo - fix queue name!!!
            //Todo - fix queue name!!!
            //Todo - fix queue name!!!


            //todo - there's a better way, but can't figure it out
            serviceCollection.Configure<RabbitMqOptions>(options => {
                options = optionsTodo;
            });

            serviceCollection.AddSingleton<IBackplaneRabbitMqConnectionFactory>((sp) =>
            {
                var factory = new ConnectionFactory { HostName = optionsTodo.Host, UserName = optionsTodo.Username, Password = optionsTodo.Password };
                return new BackplaneRabbitMqConnectionFactory(factory);
            });

            serviceCollection.AddSingleton<IBackplane>((sp) =>
            {
                var factory = sp.GetRequiredService<IBackplaneRabbitMqConnectionFactory>();                
                var connection = factory.Connection.CreateConnection();
                var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                
                //Temporary connection
                using var channel = connection.CreateModel();

                channel.ExchangeDeclare(exchange: options.ExchangeName, type: ExchangeType.Fanout);
                channel.QueueDeclare(queue: options.QueueName, exclusive: options.HideQueues, autoDelete: false);                                
                channel.QueueBind(queue: options.QueueName, exchange: options.ExchangeName, routingKey: string.Empty);
                
                logger.LogInformation($"Queue {options.QueueName} bound to {options.ExchangeName}");

                var backPlane = new Backplane(connection, options, sp.GetRequiredService<ILogger<Backplane>>());
                return backPlane;
            });

            return serviceCollection;
        }
    }

    public class RabbitMqBackPlaneOptions
    {
        internal void UsingRabbitMq(Action<RabbitMqOptions> options)
        {
            
        }
    }

    public class RabbitMqOptions
    {
        public string Host { get; set; } = "localhost";
        public string Username { get; set; } = "laptop";
        public string Password { get; set; } = "laptop";
        public string ExchangeName { get; set; } = "Notifications";
        public bool HideQueues { get; set; } = false;
        public string SubscriberName { get; set; } = Guid.NewGuid().ToString()[0..6];
        public string QueueName { get; set; } = "Notifications";
    }
}
