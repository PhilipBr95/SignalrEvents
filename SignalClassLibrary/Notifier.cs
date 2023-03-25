using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace TPT.Notification.NotifierLibrary
{
    public class Notifier<T> where T : new()
    {
        private readonly NotifierSettings _notifierSettings;
        private readonly ILogger? _logger;
        private string? _eventGroup => typeof(T).FullName;
        private HubConnection _connection;
        private T _notifierEvents;        
        private Dictionary<string, EventHandler> _eventHandlers = new Dictionary<string, EventHandler>();

        public Notifier(NotifierSettings notifierSettings, ILogger<Notifier<T>>? logger)
        {
            _notifierSettings = notifierSettings;
            _logger = logger;
        }

        public Notifier(NotifierSettings notifierSettings, ILogger? logger)
        {
            _notifierSettings = notifierSettings;
            _logger = logger;
        }

        public async Task<T> ConnectAsync()
        {
            try
            {
                var purpose = _notifierSettings.Purpose;

                if (purpose.HasFlag(NotifierPurpose.Receiver) && purpose.HasFlag(NotifierPurpose.Transmitter))
                    _logger?.LogWarning($"Having {nameof(NotifierPurpose.Receiver)} and {nameof(NotifierPurpose.Transmitter)} will cause a feedback loop!!");

                _connection = new HubConnectionBuilder()
                    .WithUrl(_notifierSettings.Url)
                    .WithAutomaticReconnect()
                .Build();

                _connection.
                _connection...Closed += async (error) =>
                {
                    _logger?.LogWarning($"Disconnected with {error}");
                    await Task.Delay(new Random().Next(0, 5) * 1000);

                    _logger?.LogWarning($"Connecting...");
                    await _connection.StartAsync();
                };

                _logger?.LogInformation($"Connecting to {_notifierSettings.Url} as a [{purpose}]");

                await _connection.StartAsync()
                                 .ContinueWith((t) => 
                                 {                    
                                     _logger?.LogInformation($"Connected...");

                                     _logger?.LogInformation($"Joining the group {_eventGroup}");
                                     _connection.InvokeAsync("JoinGroup", _eventGroup);
                                 });                

                _notifierEvents = new T();

                //Configure receiving messages
                if (purpose.HasFlag(NotifierPurpose.Receiver))                
                    _connection.On<object, string, string, object>("RaiseEvent", (sender, eventGroup, eventName, data) => PublishEventLocally(sender, eventGroup, eventName, data)); 

                //Configure sending messages
                if (purpose.HasFlag(NotifierPurpose.Transmitter))
                {
                    var events = typeof(T).GetEvents();
                    foreach (var evt in events)
                    {                        
                        var eventHandler = GetEventHandlerFor(evt);

                        if (eventHandler == null)
                            _logger?.LogWarning($"Missing EventArgs? for {evt.Name}");

                        if (_eventHandlers.ContainsKey(eventHandler.ArgumentType.FullName))
                            throw new InvalidDataException($"All EventArgs must be unique - {eventHandler.ArgumentType.FullName} is already associated with an Event");

                        _eventHandlers.Add(eventHandler.ArgumentType.FullName, eventHandler);

                        evt.AddEventHandler(_notifierEvents, eventHandler.Handler);
                    }
                }

                return _notifierEvents;
            }
            catch(Exception ex) 
            {
                _logger?.LogError(ex, $"EventGroup: {_eventGroup}, Url: {_notifierSettings.Url}");
                throw;
            }
        }        

        private EventHandler GetEventHandlerFor(EventInfo eventInfo)
        {
            try
            {
                var genericHandlerMethod = this.GetType()
                                               .GetMethod(nameof(HandleRemoteEvent), BindingFlags.NonPublic | BindingFlags.Instance);

                if (genericHandlerMethod == null)
                    throw new NullReferenceException($"Failed to find {nameof(HandleRemoteEvent)}");

                var argType = GetEventHandlerType(eventInfo);
                var handlerMethod = genericHandlerMethod.MakeGenericMethod(argType);

                return new EventHandler { 
                    Handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, handlerMethod),  
                    ArgumentType = argType,
                    EventName = eventInfo.Name
                };
            }
            catch(Exception ex) {
                _logger?.LogError(ex, $"Error creating a Handler for {eventInfo.Name}.  Probable args related!!");
                throw;
            }
        }

        /// <summary>
        /// The main handler for sending events to the server
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleRemoteEvent<TArgs>(object? sender, TArgs args) where TArgs : EventArgs
        {
            var json = _notifierSettings.Serialiser.Serialise(args);
            var handler = _eventHandlers[typeof(TArgs).FullName];

            _logger?.LogDebug($"Sending {_eventGroup}=>{handler.EventName} - {json}");

            //Don't care it's async
            _connection.InvokeAsync("RaiseEvent", sender, _eventGroup, handler.EventName, json);
        }

        /// <summary>
        /// Bubbles the server event to the client
        /// </summary>
        /// <param name="eventGroup"></param>
        /// <param name="eventName"></param>
        /// <param name="json"></param>
        public void PublishEventLocally(object sender, string eventGroup, string eventName, object data)
        {
            string json = data?.ToString();
            _logger?.LogDebug($"Received event {eventGroup}=>{eventName} from {sender} - {json}");

            var evt = typeof(T).GetEvent(eventName);

            if(evt == null)
            {
                _logger?.LogWarning($"Event {eventName} not found in {typeof(T).Name}");
                return;
            }

            var eventArg = _notifierSettings.Serialiser.Deserialise(json, GetEventHandlerType(evt));

            var delegates = GetDelegates(eventName);

            delegates?.ToList()
                      .ForEach(fe => fe.DynamicInvoke(new object[] { sender, eventArg }));
        }


        /// <summary>
        /// Events are tricky/strange things to find
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        private IEnumerable<Delegate>? GetDelegates(string eventName)
        {
            FieldInfo eventFieldInfo = typeof(T).GetField(eventName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            var multiDelegate = eventFieldInfo?.GetValue(_notifierEvents) as MulticastDelegate;

            return multiDelegate?.GetInvocationList();
        }

        private Type GetEventHandlerType(EventInfo? evt)
        {
            try
            {
                return evt?.EventHandlerType?.GenericTypeArguments[0];
            }
            catch (Exception ex) 
            {
                _logger?.LogError(ex, $"Is {evt.Name} missing an EventArgs?");
                throw;
            }          
        }
    }
}