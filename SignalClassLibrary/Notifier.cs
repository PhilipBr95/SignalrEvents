using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Reflection;

namespace SignalClassLibrary
{

    public class Notifier<T> where T : new()
    {
        private readonly NotifierSettings _notifierSettings;
        private readonly ILogger<Notifier<T>>? _logger;
        private HubConnection _connection;

        public Notifier(NotifierSettings notifierSettings, ILogger<Notifier<T>>? logger) 
        {
            _notifierSettings = notifierSettings;
            _logger = logger;
        }

        public async Task<T> ConnectAsync()
        {
            try
            {
                _connection = new HubConnectionBuilder()
                    .WithUrl(_notifierSettings.Url)
                .Build();

                _connection.Closed += async (error) =>
                {
                    await Task.Delay(new Random().Next(0, 5) * 1000);
                    await _connection.StartAsync();
                };

                await _connection.StartAsync();
                _logger?.LogInformation($"Connecting to {_notifierSettings.Url}");

                _notifierEvents = new T();
                _notifierType = typeof(T);

                var events = _notifierType.GetEvents();
                foreach (var evt in events)
                {
                    //Incoming messages
                    _connection.On<string>(evt.Name, (message) => PublishEvent(message));

                    //Outgoing messages
                    var handler = GetHandlerFor(evt);
                    evt.AddEventHandler(_notifierEvents, handler);
                }
                
                return _notifierEvents;
            }
            catch(Exception ex) 
            {
                _logger?.LogError(ex, $"Url: {_notifierSettings.Url}");
                throw;
            }
        }        

        private Delegate GetHandlerFor(EventInfo eventInfo)
        {
            try
            {
                var genericHandlerMethod = this.GetType()
                                               .GetMethod(nameof(Handler), BindingFlags.NonPublic | BindingFlags.Instance);

                if (genericHandlerMethod == null)
                    throw new NullReferenceException($"Failed to find {nameof(genericHandlerMethod)}");

                var handlerMethod = genericHandlerMethod.MakeGenericMethod(eventInfo.EventHandlerType.GenericTypeArguments[0]);
                return Delegate.CreateDelegate(eventInfo.EventHandlerType, this, handlerMethod);
            }
            catch(Exception ex) {
                _logger?.LogError(ex, $"Error creating a Handler for {eventInfo.Name}");

                throw;
            }
        }

        // zero refernces, but accessed via reflection. Do not delete!
        private void Handler<TArgs>(object? sender, TArgs args)
        {
            _logger?.LogInformation($"Sending {args}");            
        }
        
        private Type _notifierType;
        private T _notifierEvents;

        public void PublishEvent(string message)
        {
            _logger?.LogInformation("todo");
            //_notifierType.InvokeMember("", System.Reflection.BindingFlags.ev);
        }

        //internal void Raise<TEventArgs>(string eventName, TEventArgs eventArgs) where TEventArgs : EventArgs
        //{
        //    MulticastDelegate eventDelegate = (MulticastDelegate) _notifierType.GetField(eventName, BindingFlags.Instance | BindingFlags.NonPublic)
        //                                                                       .GetValue(source);
        //    if (eventDelegate != null)
        //    {
        //        foreach (var handler in eventDelegate.GetInvocationList())
        //        {
        //            handler.Method.Invoke(handler.Target, new object[] { source, eventArgs });
        //        }
        //    }
        //}

        private IEnumerable<string> GetEvents()
        {
            return _notifierType.GetEvents()
                                .Select(e => e.Name);
        }

        
        //public async Task ConnectAsync<T>(Action<T, string> onRecieve)
        //{
        //    await ConnectAsync<T>();

        //    _connection.On<T, string>(nameof(Actions.SendToAll), (clientId, message) => onRecieve(clientId, message));
        //}

        public async Task SendAsync(string clientId, string message)
        {
            await _connection.InvokeAsync(nameof(Actions.SendToAll), clientId, message);
        }
    }
}