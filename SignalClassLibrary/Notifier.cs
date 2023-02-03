using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data.Common;
using System.Reflection;

namespace SignalClassLibrary
{
    [Flags]
    public enum NotifierPurposes
    {
        Receive = 1,
        Send = 2
    }

    internal class EventHandler
    {
        public Type ArgumentType { get; internal set; }
        public Delegate Handler { get; internal set; }
    }

    public class Notifier<T> where T : new()
    {
        private readonly NotifierSettings _notifierSettings;
        private readonly ILogger? _logger;

        
        private HubConnection _connection;
        private T _notifierEvents;
        private string? _eventGroup => typeof(T).FullName;

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

        public async Task<T> ConnectAsync(NotifierPurposes purposes)
        {
            try
            {
                if (purposes.HasFlag(NotifierPurposes.Receive) && purposes.HasFlag(NotifierPurposes.Send))
                    _logger?.LogWarning($"Having {nameof(NotifierPurposes.Receive)} and {nameof(NotifierPurposes.Send)} will cause a feedback loop!!");

                _connection = new HubConnectionBuilder()
                    .WithUrl(_notifierSettings.Url)
                .Build();

                _connection.Closed += async (error) =>
                {
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    await _connection.StartAsync();
                };

                _logger?.LogInformation($"Connecting to {_notifierSettings.Url} as {purposes}");

                await _connection.StartAsync()
                                 .ContinueWith((t) => 
                                 {                    
                                     _logger?.LogInformation($"Connected...");

                                     _logger?.LogInformation($"Joining the group {_eventGroup}");
                                     _connection.InvokeAsync("JoinGroup", _eventGroup);
                                 });                

                _notifierEvents = new T();

                //Configure receiving messages
                if (purposes.HasFlag(NotifierPurposes.Receive))                
                    _connection.On<string, string, string>("RaiseEvent", (eventGroup, eventName, json) => PublishEventLocally(eventGroup, eventName, json)); //Don't like RaiseEvent :-(

                //Configure sending messages
                if (purposes.HasFlag(NotifierPurposes.Send))
                {
                    var events = typeof(T).GetEvents();
                    foreach (var evt in events)
                    {                        
                        var eventHandler = GetEventHandlerFor(evt);
                        _eventHandlers.Add(evt.Name, eventHandler);

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
                    throw new NullReferenceException($"Failed to find {nameof(genericHandlerMethod)}");

                var argType = GetEventHandlerType(eventInfo);
                var handlerMethod = genericHandlerMethod.MakeGenericMethod(GetEventHandlerType(eventInfo));

                return new EventHandler { 
                    Handler = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, handlerMethod),  
                    ArgumentType = argType
                };
            }
            catch(Exception ex) {
                _logger?.LogError(ex, $"Error creating a Handler for {eventInfo.Name}");

                throw;
            }
        }

        /// <summary>
        /// The main handler for sending events to the server
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleRemoteEvent<TArgs>(object? sender, TArgs args)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(args);            
            var handler = _eventHandlers.First();

            _logger?.LogInformation($"Sending {_eventGroup}=>{handler.Key} - {json}");

            //Todo async!!
            _connection.InvokeAsync("RaiseEvent", _eventGroup, handler.Key, json).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Bubbles the server event to the client
        /// </summary>
        /// <param name="eventGroup"></param>
        /// <param name="eventName"></param>
        /// <param name="json"></param>
        public void PublishEventLocally(string eventGroup, string eventName, string json)
        {
            _logger?.LogInformation($"Received {eventGroup}=>{eventName} - {json}");

            var evt = typeof(T).GetEvent(eventName);

            if(evt == null)
            {
                _logger?.LogInformation($"Event {eventName} not found in {typeof(T).Name}");
                return;
            }

            var eventArg = System.Text.Json.JsonSerializer.Deserialize(json, GetEventHandlerType(evt));

            var delegates = GetDelegates(eventName);

            delegates?.ToList()
                      .ForEach(fe => fe.DynamicInvoke(new object[] { eventName, eventArg }));
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

        private static Type GetEventHandlerType(EventInfo? evt)
        {
            return evt.EventHandlerType.GenericTypeArguments[0];
        }
    }
}