using ClassLibrary1;
using SignalClassLibrary;

namespace ConsoleAppClient
{
    public class ProgramClient
    {
        private static Notifier<CalculationNotifications> _notifier;
        
        static async Task Main(string[] args)
        {            
            Console.WriteLine("Hello, Client 1.  Press Enter to connect");
            Console.ReadLine();

            _notifier = new Notifier<CalculationNotifications>(new NotifierSettings { Url = "https://localhost:7061/ChatHub" }, null);
            
            var notifications = await _notifier.ConnectAsync();
            notifications.OnStarted += Notifications_Started1; ;
            
            while (true)
            { 
                Console.WriteLine("Client 1 Running - Press something to send");
                Console.ReadLine();

                Console.WriteLine("Sending...");
                await _notifier.SendAsync("ClientId:1", "Hello");
            }
        }

        private static void Notifications_Started1(object? sender, StartedEventArgs e)
        {
            Console.WriteLine("Started");
        }

    }

}