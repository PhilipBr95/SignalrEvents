using System.IO;
using System.Reflection;

namespace ClassLibrary1
{

    public class CalculationNotificationEventArgs : EventArgs
    {
        public int TargetId { get; set; }
    }

    public class StartedEventArgs : CalculationNotificationEventArgs { }
    public class ErroredEventArgs: CalculationNotificationEventArgs { }
    public class FinishedEventArgs : CalculationNotificationEventArgs { }
 
    public class CalculationNotifications
    {
        public event EventHandler<StartedEventArgs> Started;
        public event EventHandler<FinishedEventArgs> Finished;
        public event EventHandler<ErroredEventArgs> Errored;     //I know it's poor grammer
        
        public void RaiseStarted(object sender, StartedEventArgs e)
        {            
            Started?.Invoke(sender, e);
        }
        public void RaiseFinished(object sender, FinishedEventArgs e)
        {
            Finished?.Invoke(sender, e);
        }
        public void RaiseErrored(object sender, ErroredEventArgs e)
        {
            Errored?.Invoke(sender, e);
        }
    }

    public class TreeNotifications
    {
        public event EventHandler<StartedEventArgs> Started;
        public event EventHandler<FinishedEventArgs> Finished;

        public void RaiseStarted(object sender, StartedEventArgs e)
        {
            var dd = Started.GetInvocationList();

            Started?.Invoke(sender, e);
        }
        public void RaiseFinished(object sender, FinishedEventArgs e)
        {
            Finished?.Invoke(sender, e);
        }
    }
}