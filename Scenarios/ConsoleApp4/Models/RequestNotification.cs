namespace ConsoleApp4.Models
{
    public class RequestNotification
    {
        public event EventHandler<ProcessingRequiredEventArgs> ProcessingRequired;

        public void RaiseProcessingRequired(object sender, ProcessingRequiredEventArgs e)
        {
            ProcessingRequired?.Invoke(sender, e);
        }

        public class ProcessingRequiredEventArgs : EventArgs
        {
            public int RequestId { get; set; }
        }
    }
}