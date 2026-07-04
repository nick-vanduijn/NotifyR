# Overview

NotifyR is a lightweight mediator library for .NET, inspired by MediatR. It enables in-process message passing with support for requests (command/query), notifications (events), and pipeline behaviors (middleware).

## Features

- **Request/Response** — Send a request and get a typed response back
- **Void requests** — Fire-and-forget requests with no return value
- **Notifications** — Fire-and-forget events consumed by zero or more handlers
- **Pipeline Behaviors** — Cross-cutting concerns (logging, validation, caching) via middleware
- **DI-first** — Built on `Microsoft.Extensions.DependencyInjection`
- **Minimal overhead** — Hot path has no reflection, no closure allocations, no type checks

## NuGet

```
dotnet add package NotifyR
```

## Namespace

```csharp
using NotifyR;
```
