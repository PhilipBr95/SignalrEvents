namespace Notification.NotificationServer.Backplane.RabbitMq.Models
{
    public class RabbitMqOptions
    {
        public string Host { get; set; } 
        public string Username { get; set; } 
        public string Password { get; set; } 
        public string ExchangeName { get; set; } 
        public bool HideQueues { get; set; } = false;        
        
        /// <summary>
        /// Must be unique!!!
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Format options: ExchangeName, Guid, Hostname
        /// Default: {ExchangeName}_{Guid}
        /// </summary>
        public string QueueNameFormat { get; set; } = "{ExchangeName}_{Hostname}_{Guid}";
        public bool AutoDelete { get; set; } = true;
    }
}
