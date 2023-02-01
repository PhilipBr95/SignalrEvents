using System.IO;

namespace ClassLibrary1
{
    public class StartedEventArgs : EventArgs
    {
        public int TargetId { get; set; }
    }
    public class ProcessingEventArgs : EventArgs
    {
        public int TargetId { get; set; }
    }


    public class CalculationNotifications
    {
        public event EventHandler<StartedEventArgs> OnStarted;
        public event EventHandler<ProcessingEventArgs> OnProcessing;
        //public event EventHandler<FinishedEventArgs> OnFinished;
        //public event EventHandler<ErroredEventArgs> OnErrored;     //I know it's not right
        
        public void RaiseStarted(object sender, StartedEventArgs e)
        {
            OnStarted?.Invoke(sender, e);
        }
    }
}