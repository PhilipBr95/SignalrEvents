using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Notification.NotificationServer.RabbitMqBackplane.Models;
using Notification.NotificationServer.Backplane.Interfaces;
using Notification.NotificationServer.RabbitMqBackplane.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Notification.NotificationServer.RabbitMqBackplane.Extensions
{
    public static class SignalRBuilderExtensions
    {
        public static ISignalRBuilder AddRabbitMqBackplane(this ISignalRBuilder signalRBuilder, IConfigurationSection configurationSection)
        {
            signalRBuilder.Services.AddOptions<RabbitMqOptions>()
                                   .Bind(configurationSection)
                                   .PostConfigure(opt =>
                                   {
                                       //We need a unique QueueName
                                       if (string.IsNullOrWhiteSpace(opt.QueueName))
                                       {
                                           if (string.IsNullOrWhiteSpace(opt.QueueNameFormat))
                                               throw new InvalidDataException($"Missing {nameof(opt.QueueName)} or {nameof(opt.QueueNameFormat)}");

                                           opt.QueueName = FormatQueueName(opt.QueueNameFormat, opt.ExchangeName);
                                       }
                                   });

            signalRBuilder.Services.AddSingleton<IRabbitMqConnectionFactory>((sp) =>
            {
                var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                var factory = new ConnectionFactory { HostName = options.Host, UserName = options.Username, Password = options.Password };
                return new BackplaneRabbitMqConnectionFactory(factory);
            });

            signalRBuilder.Services.AddSingleton<IBackplane>((sp) =>
            {
                var factory = sp.GetRequiredService<IRabbitMqConnectionFactory>();
                var connection = factory.Connection.CreateConnection();
                var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                var logger = sp.GetRequiredService<ILogger<ISignalRBuilder>>();

                //Temporary connection to create Queues
                using var channel = connection.CreateModel();

                channel.ExchangeDeclare(exchange: options.ExchangeName, type: ExchangeType.Fanout);
                channel.QueueDeclare(queue: options.QueueName, exclusive: options.HideQueues, autoDelete: options.AutoDelete);
                channel.QueueBind(queue: options.QueueName, exchange: options.ExchangeName, routingKey: string.Empty);

                logger.LogInformation($"Queue {options.QueueName} bound to {options.ExchangeName}");

                var backPlane = new Backplane(connection, options, sp.GetRequiredService<ILogger<Backplane>>());
                return backPlane;
            });

            return signalRBuilder;
        }

        private static string FormatQueueName(string queueNameFormat, string exchangeName)
        {
            return queueNameFormat.Replace("{ExchangeName}", exchangeName)
                                  .Replace("{Guid}", Guid.NewGuid().ToString()[0..8])
                                  .Replace("{Hostname}", System.Net.Dns.GetHostName());
        }
    }
}
