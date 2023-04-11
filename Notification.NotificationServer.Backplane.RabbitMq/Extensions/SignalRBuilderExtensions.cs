using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using Notification.NotificationServer.Backplane.RabbitMq.Models;
using Notification.NotificationServer.Backplane.Interfaces;
using Notification.NotificationServer.Backplane.RabbitMq.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Notification.NotificationServer.Backplane.RabbitMq.Extensions
{
    public static class SignalRBuilderExtensions
    {
        public static ISignalRBuilder AddRabbitMqBackplane<THub>(this ISignalRBuilder signalRBuilder, IConfigurationSection configurationSection) where THub : Hub
        {
            AddRabbitMqConfig(signalRBuilder, configurationSection);

            LogBackplaneConfig(signalRBuilder);

            AddRabbitMq(signalRBuilder);

            ConfigureRabbitMqBackplane<THub>(signalRBuilder);

            return signalRBuilder;
        }

        private static void AddRabbitMqConfig(ISignalRBuilder signalRBuilder, IConfigurationSection configurationSection)
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
        }

        private static void ConfigureRabbitMqBackplane<THub>(ISignalRBuilder signalRBuilder) where THub : Hub
        {
            signalRBuilder.Services.TryAddSingleton<IBackplane<THub>>((sp) =>
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

                var hubContext = sp.GetRequiredService<IHubContext<THub>>();
                var backPlane = new Backplane<THub>(hubContext, connection, options, sp.GetRequiredService<ILogger<IBackplane<THub>>>());

                return backPlane;
            });
        }

        private static void AddRabbitMq(ISignalRBuilder signalRBuilder)
        {
            signalRBuilder.Services.TryAddSingleton<IRabbitMqConnectionFactory>((sp) =>
            {
                var options = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
                var factory = new ConnectionFactory { HostName = options.Host, UserName = options.Username, Password = options.Password };
                return new BackplaneRabbitMqConnectionFactory(factory);
            });
        }

        private static void LogBackplaneConfig(ISignalRBuilder signalRBuilder)
        {
            var tempSp = signalRBuilder.Services.BuildServiceProvider();
            var opt = tempSp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
            
            var logger = tempSp.GetRequiredService<ILoggerFactory>()
                               .CreateLogger(typeof(SignalRBuilderExtensions));

            logger.LogInformation($"Using '{opt.Host}' for the Backplane");
        }

        private static string FormatQueueName(string queueNameFormat, string exchangeName)
        {
            return queueNameFormat.Replace("{ExchangeName}", exchangeName)
                                  .Replace("{Guid}", Guid.NewGuid().ToString()[0..8])
                                  .Replace("{Hostname}", System.Net.Dns.GetHostName());
        }
    }
}
