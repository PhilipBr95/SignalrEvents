using ClassLibrary1;

namespace ConsoleApp4.Models
{
    internal class Request
    {
        public int RequestId { get; set; }
        public NotificationSettings NotificationSettings { get; set; }
        public CalculationNotification Events { get; set; }
    }
}