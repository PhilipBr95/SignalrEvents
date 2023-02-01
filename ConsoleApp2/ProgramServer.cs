using ClassLibrary1;
using SignalClassLibrary;

namespace ConsoleAppServer
{
    internal class ProgramServer
    {
        private static Notifier<CalculationNotifications> _notifier;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, Server 1.  Press Enter to connect");
            Console.ReadLine();

            _notifier = new Notifier<CalculationNotifications>(new NotifierSettings { Url = "https://localhost:7061/ChatHub" }, null);

            var notifications = await _notifier.ConnectAsync();

            Console.WriteLine("Press Enter to send");
            Console.ReadLine();

            notifications.OnStarted += Notifications_OnStarted;
            notifications.RaiseStarted(null, new StartedEventArgs { TargetId = 555 });

            Console.WriteLine("Server 1 Running - Don't press anything");
            Console.ReadLine();
        }

        private static void Notifications_OnStarted(object? sender, StartedEventArgs e)
        {
            Console.WriteLine($"Hello {e.TargetId}");
        }
    }
}