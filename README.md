# SignalrEvents

This repo provides functionality to create remote events; So an event raised in one system can be consumed in another.  

It also implements a RabbitMq backplane to allow the SignalR Notification server to run in an appfarm.

**Consuming and Raising events should be handled separately!!**

## Consuming Events:
```csharp
    var notifier = new Notifier<MyEvent>(new NotifierSettings(_server, NotifierPurpose.Receiver, null), _logger);
    var notifications = await notifier.ConnectAsync();

    notifications.ProcessingRequired += (sender, request) =>
    {
        _logger.LogInformation($"Received 'RequestId {request.RequestId}'");
    };
```

## Raising Events:
```csharp
    var notifier = new Notifier<MyEvent>(new NotifierSettings(_server, NotifierPurpose.Transmitter, null), _logger);
    var notifications = await notifier.ConnectAsync();
        
    notifications.RaiseStarted(new object(), new StartedEventArgs { RequestId = request.RequestId });
```

## Private Messages
Private messages should be handled via the EventArgs.

## How To
View the Scenarios folder for usage examples.