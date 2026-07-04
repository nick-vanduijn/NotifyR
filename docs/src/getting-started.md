# Getting Started

## 1. Define a request and response

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

For void requests (no return value), use `IRequest`:

```csharp
public class Ping : IRequest;
```

## 2. Create a handler

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

For a void request, implement `IRequestHandler<TRequest>`:

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

## 3. Wire it up

```csharp
var services = new ServiceCollection();

services.AddNotifyR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<GetUser>();
});

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();
```

## 4. Send

```csharp
// Typed response
var user = await mediator.Send(new GetUser { UserId = 42 });

// Void request
await mediator.Send(new Ping());
```

## 5. Define and publish a notification

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
