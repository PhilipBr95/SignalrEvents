using System;

namespace Notification.NotifierLibrary
{
    internal class EventHandler
    {
        public Type ArgumentType { get; internal set; }
        public Delegate Handler { get; internal set; }
        public string EventName { get; internal set; }
    }
}