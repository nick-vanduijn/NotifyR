# Getting Started

The quickest way to understand NotifyR is to build one request, one handler, and one mediator instance. The pattern is intentionally simple: define a request type, implement a handler for that request, register the assembly with `AddNotifyR`, and then send the request through `IMediator`.

## Define a request and response

Start with a request type that describes the work you want done. If the operation returns a value, use `IRequest<TResponse>` so the response type is part of the contract.

```csharp
public class GetUser : IRequest<GetUserResponse>
{
    public int UserId { get; init; }
}

public class GetUserResponse
{
    public string Name { get; init; } = "";
}
```

If the operation does not return data, use `IRequest`. This keeps the API consistent while still making the request explicit.

```csharp
public class Ping : IRequest;
```

## Create a handler

Handlers hold the actual logic for the request. A handler should be focused on one operation and should return the response directly rather than hiding the work behind framework code.

```csharp
public class GetUserHandler : IRequestHandler<GetUser, GetUserResponse>
{
    public Task<GetUserResponse> Handle(GetUser request, CancellationToken ct)
    {
        var response = new GetUserResponse { Name = $"User_{request.UserId}" };
        return Task.FromResult(response);
    }
}
```

For a void request, implement `IRequestHandler<TRequest>`. The return value is `Unit`, which gives the handler a consistent signature without forcing an artificial response object.

```csharp
public class PingHandler : IRequestHandler<Ping>
{
    public Task<Unit> Handle(Ping request, CancellationToken ct)
    {
        Console.WriteLine("Ping received!");
        return Unit.Completed;
    }
}
```

## Wire it up

NotifyR is registered through the Microsoft dependency injection container. Most applications scan an assembly that contains handlers and then resolve `IMediator` from the service provider.

```csharp
var services = new ServiceCollection();

services.AddNotifyR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<GetUser>();
});

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();
```

## Send requests

Once the mediator is available, sending a request is just another asynchronous operation. Typed requests return the declared response, and void requests complete once the handler has finished.

```csharp
// Typed response
var user = await mediator.Send(new GetUser { UserId = 42 });

// Void request
await mediator.Send(new Ping());
```

## Publish notifications

Notifications are useful when one event should trigger more than one reaction. Each handler is resolved from dependency injection, and every matching handler gets a chance to respond to the same notification.

```csharp
public class UserCreated : INotification
{
    public int UserId { get; init; }
}

public class LogUserCreated : INotificationHandler<UserCreated>
{
    public Task Handle(UserCreated notification, CancellationToken ct)
    {
        Console.WriteLine($"Log: User {notification.UserId} created");
        return Task.CompletedTask;
    }
}

await mediator.Publish(new UserCreated { UserId = 7 });
```

This style scales well because the publisher does not need to know which reactions exist. New behavior can be added by registering another handler, which keeps the notification source stable as the application grows.
