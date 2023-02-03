namespace SignalClassLibrary
{
    internal class EventHandler
    {
        public Type ArgumentType { get; internal set; }
        public Delegate Handler { get; internal set; }
    }
}