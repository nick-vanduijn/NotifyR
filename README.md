# NotifyR

[![NuGet](https://img.shields.io/nuget/v/NotifyR.svg?style=for-the-badge)](https://www.nuget.org/packages/NotifyR)
[![License](https://img.shields.io/github/license/nick-vanduijn/NotifyR.svg?style=for-the-badge)](LICENSE)
[![Docs](https://img.shields.io/badge/docs-online-blue.svg?style=for-the-badge)](https://nick-vanduijn.github.io/NotifyR)

<a id="readme-top"></a>

<div align="center">
  <h3 align="center">NotifyR</h3>
  <p align="center">
    A lightweight mediator library for .NET with support for requests, notifications, and pipeline behaviors.
    <br />
    <a href="https://nick-vanduijn.github.io/NotifyR"><strong>Read the docs »</strong></a>
    <br />
    <br />
    <a href="https://www.nuget.org/packages/NotifyR">View NuGet Package</a>
    ·
    <a href="https://github.com/nick-vanduijn/NotifyR/issues">Report Bug</a>
    ·
    <a href="https://github.com/nick-vanduijn/NotifyR/issues/new/choose">Request Feature</a>
  </p>
</div>

## Table of Contents

- [About](#about)
- [Built With](#built-with)
- [Getting Started](#getting-started)
- [Usage](#usage)
- [License](#license)
- [Contact](#contact)

## About

NotifyR is a DI-first mediator implementation for .NET inspired by MediatR. It provides in-process message passing with support for typed requests, void requests, notifications, and pipeline behaviors.

### Features

- Request/response handlers with compile-time type safety
- Void requests for fire-and-forget workflows
- Notifications with one or more handlers
- Pipeline behaviors for cross-cutting concerns such as logging, validation, and caching
- Minimal overhead on the hot path

## Built With

- .NET 10
- `Microsoft.Extensions.DependencyInjection`

## Getting Started

### Prerequisites

- .NET SDK 10.0 or later

### Installation

```bash
dotnet add package NotifyR
```

### Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;
using NotifyR;

var services = new ServiceCollection();

services.AddNotifyR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<GetUser>();
});

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<IMediator>();
```

## Usage

### Requests

```csharp
public class GetUser : IRequest<GetUserResponse>
{
    public int UserId { get; init; }
}

public class GetUserResponse
{
    public string Name { get; init; } = string.Empty;
}

public class GetUserHandler : IRequestHandler<GetUser, GetUserResponse>
{
    public Task<GetUserResponse> Handle(GetUser request, CancellationToken ct)
    {
        return Task.FromResult(new GetUserResponse { Name = $"User_{request.UserId}" });
    }
}

var user = await mediator.Send(new GetUser { UserId = 42 });
```

### Void Requests

```csharp
public class Ping : IRequest { }

public class PingHandler : IRequestHandler<Ping>
{
    public Task<Unit> Handle(Ping request, CancellationToken ct)
    {
        Console.WriteLine("Ping received!");
        return Unit.Completed;
    }
}

await mediator.Send(new Ping());
```

### Notifications

```csharp
public class UserCreated : INotification
{
    public int UserId { get; init; }
}

public class LogUserCreated : INotificationHandler<UserCreated>
{
    public Task Handle(UserCreated notification, CancellationToken ct)
    {
        Console.WriteLine($"User created: {notification.UserId}");
        return Task.CompletedTask;
    }
}

await mediator.Publish(new UserCreated { UserId = 7 });
```

### Pipeline Behaviors

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TRequest, TResponse> next,
        CancellationToken ct)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        var response = await next(request, ct);
        Console.WriteLine($"Handled {typeof(TRequest).Name}");
        return response;
    }
}
```

## License

Distributed under the MIT License. See [LICENSE](LICENSE) for details.

## Contact

Project Link: https://github.com/nick-vanduijn/NotifyR

Documentation: https://nick-vanduijn.github.io/NotifyR

<p align="right"><a href="#readme-top">back to top</a></p>
