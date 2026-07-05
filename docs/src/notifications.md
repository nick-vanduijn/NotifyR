# Notifications

## `INotification`

`INotification` marks a message as something that can be published to multiple consumers. Notifications usually represent facts that have already happened, such as a user being created or an order being submitted.

```csharp
public interface INotification { }

public class UserCreated : INotification
{
    public int UserId { get; init; }
}
```

## `INotificationHandler<TNotification>`

Implement this interface when a notification should trigger a reaction. Unlike request handlers, notification handlers do not return a value. Their job is to perform the side effect associated with the event.

```csharp
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
```

Multiple handlers can subscribe to the same notification type. This is where notifications become especially useful: the publisher stays simple while new reactions can be added independently.

```csharp
public class LogUserCreated : INotificationHandler<UserCreated> { /* ... */ }
public class EmailUserCreated : INotificationHandler<UserCreated> { /* ... */ }
```

## How `Publish` works

When you call `IMediator.Publish(notification)`, NotifyR resolves the wrapper for the notification type and then asks dependency injection for every matching handler. Those handlers are invoked sequentially so the library can preserve predictable execution order and respect cancellation between each step.

The dispatch model is deliberately simple. There is no hidden event bus or background queue in the middle of the call. The publish operation runs in-process, which means the notification completes only after the subscribed handlers have finished.

## Error handling

Error handling is designed to preserve as much information as possible. If only one handler fails, NotifyR rethrows that failure directly. If several handlers fail, it wraps them in an `AggregateException` so the caller can see the full set of problems instead of only the first one.

Handlers continue executing even when one of them throws. That behavior matters when the handlers represent independent side effects, such as audit logging and email delivery, because one failure should not prevent the others from running unless cancellation has been requested. The exception (or `AggregateException`) is surfaced to the caller only after all handlers have completed.

## Registration

Handlers are usually discovered through assembly scanning. That keeps registration close to the application boundary and avoids manually registering every notification handler one by one.

```csharp
services.AddNotifyR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<MyHandler>();
});
```

If you scan the assembly that contains your handlers, NotifyR will discover the matching `INotificationHandler<TNotification>` implementations and register them using the configured lifetime.
