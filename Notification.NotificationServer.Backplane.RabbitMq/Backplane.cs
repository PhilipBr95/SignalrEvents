using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Notification.NotificationServer.Backplane.Models;
using Notification.NotificationServer.Backplane.RabbitMq.Models;
using Microsoft.AspNetCore.SignalR;
using Notification.NotificationServer.Backplane.Interfaces;

namespace Notification.NotificationServer.Backplane.RabbitMq
{
    public class Backplane<THub, TMessage> : IBackplane<THub, TMessage> where THub : Hub
                                                                        where TMessage : class
    {
        private readonly IConnection _connection;
        private readonly RabbitMqOptions _options;
        private readonly ILogger<IBackplane<THub, TMessage>> _logger;
        private readonly IHubContext<THub> _hubContext;
        private readonly IModel _channel;
        private Action<object, IHubContext<THub>, BackplaneEvent<TMessage>>? _receivedHandler = null;

        private static int _messageCounter = 0;
        public string ConsumerTag { get; private set; }

        public Backplane(IHubContext<THub> hubContext, IConnection connection, RabbitMqOptions options, ILogger<IBackplane<THub, TMessage>> logger)
        {
            _connection = connection;
            _options = options;
            _logger = logger;
            _hubContext = hubContext;

            try
            {
                _channel = _connection.CreateModel();
                Subscribe();
            }
            catch(Exception ex)
            {
                _logger.LogCritical(ex, $"Failed to connect to Rabbit");
            }
        }

        private void Subscribe()
        {            
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                byte[] body = ea.Body.ToArray();
                var json = Encoding.UTF8.GetString(body);
                var message = JsonSerializer.Deserialize<BackplaneMessage<TMessage>>(json);

                //We don't care about the message if it came from us
                if (message != null && message.MessageId.StartsWith(_options.QueueName) == false)
                {
                    _logger?.LogDebug($"Received message {message.MessageId} from RabbitMq Backplane");

                    _receivedHandler?.Invoke(this, _hubContext, new BackplaneEvent<TMessage>(message));
                }
                else
                    _logger?.LogDebug($"Ignoring message {message.MessageId} from RabbitMq Backplane");
            };

            ConsumerTag = _channel.BasicConsume(queue: _options.QueueName,
                                              autoAck: true,
                                             consumer: consumer);

            _logger.LogInformation($"Connected with ConsumerTag {ConsumerTag}");
        }

        public void Send(string connectionId, string command, TMessage message)
        {
            var backplaneMessage = new BackplaneMessage<TMessage> { ConnectionId = connectionId, Command = command, EventArgs = message, MessageId = GenerateMessageId() };
            _logger.LogDebug($"Sending message {backplaneMessage.MessageId} to the RabbitMq Backplane");

            try
            {                                
                var messageJson = JsonSerializer.Serialize(backplaneMessage);
                var body = Encoding.UTF8.GetBytes(messageJson);

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

        public void AddReceived(Action<object, IHubContext<THub>, BackplaneEvent<TMessage>> value) => _receivedHandler = value;

    }
}
