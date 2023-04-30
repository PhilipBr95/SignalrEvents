
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Notification.NotifierLibrary
{
    public class Notifier<T> : IAsyncDisposable where T : new()
    {
        private readonly NotifierSettings _notifierSettings;
        private readonly ILogger? _logger;
        private string? _eventGroup => typeof(T).FullName;
        private HubConnection _connection;
        private T _notifierEvents;        
        private Dictionary<string, EventHandler> _eventHandlers = new Dictionary<string, EventHandler>();
        private NotifierPurpose _purpose;

        public string? ConnectionId => _connection.ConnectionId;

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
                _purpose = _notifierSettings.Purpose;

                if (_purpose.HasFlag(NotifierPurpose.Receiver) && _purpose.HasFlag(NotifierPurpose.Transmitter))
                    _logger?.LogWarning($"Having {nameof(NotifierPurpose.Receiver)} and {nameof(NotifierPurpose.Transmitter)} will cause a feedback loop!!");

                _connection = new HubConnectionBuilder()
                    .WithUrl(_notifierSettings.Url)
                    .WithAutomaticReconnect()
                .Build();

                _connection.Reconnecting += async (error) =>
                {
                    if (error == null)
                    {
                        _logger?.LogDebug($"Reconnecting for unknown reason");
                        return;
                    }

                    _logger?.LogWarning($"Reconnecting due to {error}");
                    await Task.CompletedTask;
                };

                _connection.Reconnected += async (connectionId) =>
                {
                    _logger?.LogDebug($"Reconnected...");

                    await JoinGroupAsync();
                };

                _connection.Closed += async (error) =>
                {
                    if (error == null)
                    {
                        _logger?.LogDebug($"Safely Disconnected");
                        return;
                    }

                    _logger?.LogWarning($"Disconnected with {error}");
                    await Task.Delay(new Random().Next(0, 5) * 1000);

                    _logger?.LogWarning($"Unexpectedly Closed...");
                    await ConnectAndJoinGroupAsync();
                };

                await ConnectAndJoinGroupAsync();
                
                _notifierEvents = new T();

                //Configure receiving messages
                if (_purpose.HasFlag(NotifierPurpose.Receiver))
                    _connection.On<NotifierEventArgs>("RaiseEvent", (myArgs) => RaiseLocalEvent(myArgs));

                //Configure sending messages
                if (_purpose.HasFlag(NotifierPurpose.Transmitter))
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
            catch (Exception ex) 
            {
                _logger?.LogError(ex, $"EventGroup: {_eventGroup}, Url: {_notifierSettings.Url}");
                throw;
            }
        }

        private async Task ConnectAndJoinGroupAsync()
        {
            _logger?.LogInformation($"Connecting to {_notifierSettings.Url} as a [{_purpose}] for {_eventGroup}");

            await _connection.StartAsync()
                             .ContinueWith(async (t) => await JoinGroupAsync());

            if (_connection.State == HubConnectionState.Disconnected)
                throw new Exception($"Failed to connect to {_notifierSettings.Url}");
        }

        private async Task JoinGroupAsync()
        {
            if (_connection.State != HubConnectionState.Connected)
                throw new InvalidOperationException("Not Connected!!");

            _logger?.LogInformation($"Connected as {_connection.ConnectionId} for {_eventGroup}");

            _logger?.LogInformation($"Joining the group '{_eventGroup}'");
            await _connection.InvokeAsync("JoinGroup", _eventGroup);
        }

        private EventHandler GetEventHandlerFor(EventInfo eventInfo)
        {
            try
            {
                var genericHandlerMethod = this.GetType()
                                               .GetMethod(nameof(RaiseRemoteEvent), BindingFlags.NonPublic | BindingFlags.Instance);

                if (genericHandlerMethod == null)
                    throw new NullReferenceException($"Failed to find {nameof(RaiseRemoteEvent)}");

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
        private void RaiseRemoteEvent<TArgs>(object? sender, TArgs args) where TArgs : EventArgs
        {
            try
            {
                var json = _notifierSettings.Serialiser.Serialise(args);
                var handler = _eventHandlers[typeof(TArgs).FullName];

                _logger?.LogInformation($"Sending {_eventGroup}=>{handler.EventName} - {json} to {_notifierSettings.Url}");

                //It's async - don't care about waiting
                _ = _connection.InvokeAsync("RaiseEvent", new NotifierEventArgs { Sender = sender?.ToString(), EventGroup = _eventGroup, EventName = handler.EventName, Json = json });
            }
            catch(Exception ex)
            {
                _logger?.LogError(ex, $"Error with {typeof(TArgs).FullName}");
                throw;
            }
        }

        /// <summary>
        /// Bubbles the server event to the client
        /// </summary>
        /// <param name="myArgs"></param>
        public void RaiseLocalEvent(NotifierEventArgs myArgs)
        {
            _logger?.LogInformation($"Received event {myArgs.EventGroup}=>{myArgs.EventName} from {myArgs.Sender} - {myArgs.Json}");

            var evt = typeof(T).GetEvent(myArgs.EventName);

            if (evt == null)
            {
                _logger?.LogWarning($"Event {myArgs.EventName} not found in {typeof(T).Name}");
                return;
            }

            var eventArg = _notifierSettings.Serialiser.Deserialise(myArgs.Json, GetEventHandlerType(evt));

            var delegates = GetDelegates(myArgs.EventName);

            delegates?.ToList()
                      .ForEach(fe => fe.DynamicInvoke(new object[] { myArgs.Sender, eventArg }));
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

        public async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync();
            
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
    }
}