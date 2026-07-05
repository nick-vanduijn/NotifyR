# NotifyR

A lightweight mediator implementation for .NET with support for requests, notifications, and pipeline behaviors.

## Install

```
dotnet add package NotifyR
```

## Quick start

```csharp
services.AddNotifyR(cfg =>
    cfg.RegisterServicesFromAssemblyContaining<MyHandler>());
```

See the [documentation](https://nick-vanduijn.github.io/NotifyR) for more details.
