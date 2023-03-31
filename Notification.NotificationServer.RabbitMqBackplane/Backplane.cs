using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Notification.NotificationServer.Backplane.Interfaces;
using Notification.NotificationServer.Backplane.Models;
using Notification.NotificationServer.RabbitMqBackplane.Models;

namespace Notification.NotificationServer.RabbitMqBackplane
{
    public class Backplane : IBackplane
    {
        private IConnection _connection;
        private RabbitMqOptions _options;
        private ILogger<Backplane> _logger;
        private IModel _channel;

        private static int _messageCounter = 0;

        public event EventHandler<BackplaneEvent>? Received;

        public Backplane(IConnection connection, RabbitMqOptions options, ILogger<Backplane> logger)
        {
            _connection = connection;
            _options = options;
            _logger = logger;

            _channel = _connection.CreateModel();
            Subscribe();
        }

        private void Subscribe()
        {            
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<BackplaneMessage>(json);

                //We don't care about the message if it came from us
                if (message != null && message.MessageId.StartsWith(_options.QueueName) == false)
                    Received?.Invoke(this, new BackplaneEvent(message));
                else
                    _logger?.LogDebug($"Ignore message {message.MessageId}");
            };

            _channel.BasicConsume(queue: _options.QueueName,
                                 autoAck: true,
                                 consumer: consumer);
        }

        public void Send(string command, MessageData messageData)
        {
            var backplaneMessage = new BackplaneMessage { Command = command, MessageData = messageData, MessageId = GenerateMessageId() };
            _logger.LogInformation($"Sending {backplaneMessage.MessageId} to the RabbitMq Backplane");

            try
            {                                
                var message = JsonSerializer.Serialize(backplaneMessage);
                var body = Encoding.UTF8.GetBytes(message);

                using var model = _connection.CreateModel();
                model.ConfirmSelect();
                model.BasicPublish(exchange: _options.ExchangeName,
                                     routingKey: string.Empty,
                                     basicProperties: null,
                                     body: body);
                model.WaitForConfirmsOrDie();
                model.Close();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error sending {backplaneMessage.MessageId}");
                throw;
            }
        }

        private string GenerateMessageId()
        {
            var id = Interlocked.Increment(ref _messageCounter);
            return $"{_options.QueueName}_{id}";
        }
    }
}
