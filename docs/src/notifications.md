# Notifications

## `INotification`

A marker interface for notification messages:

```csharp
public interface INotification { }

public class UserCreated : INotification
{
    public int UserId { get; init; }
}
```

## `INotificationHandler<TNotification>`

Implement this to handle a notification:

```csharp
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
```

Multiple handlers can subscribe to the same notification type:

```csharp
public class LogUserCreated : INotificationHandler<UserCreated> { /* ... */ }
public class EmailUserCreated : INotificationHandler<UserCreated> { /* ... */ }
```

## How `Publish` works

1. `IMediator.Publish(notification)` is called
2. `WrapperCache` looks up or creates a `NotificationHandlerWrapperImpl<TNotification>`
3. The wrapper resolves ALL `INotificationHandler<TNotification>` from DI
4. Each handler is invoked sequentially
5. Cancellation is checked before each handler
6. Exceptions are collected, not thrown immediately

## Error handling

- If a single handler throws, that exception is rethrown unwrapped
- If multiple handlers throw, an `AggregateException` wrapping all exceptions is thrown
- Handlers continue executing even if a previous handler throws — all handlers get a chance to run

## Registration

Handlers are auto-discovered via assembly scanning and registered with the configured lifetime:

```csharp
services.AddNotifyR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<MyHandler>();
});
```
