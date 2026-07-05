# Overview

NotifyR is a lightweight mediator library for .NET, inspired by MediatR. It gives you a simple way to move application logic away from controllers, UI handlers, and other entry points and into focused request handlers, notification handlers, and pipeline behaviors. The result is a codebase that is easier to read, easier to test, and easier to grow as the application gets more complicated.

At a high level, NotifyR provides in-process message passing. A request represents a single operation that expects a response. A notification represents a domain event or side effect that may have one or many consumers. Pipeline behaviors sit around request handling and let you add cross-cutting concerns such as logging, validation, caching, or timing without copying that logic into every handler.

The library is intentionally small. It builds on `Microsoft.Extensions.DependencyInjection`, uses familiar .NET abstractions, and keeps the hot path lean so the mediator layer stays out of the way of the actual business logic.

## Core concepts

Requests are the primary entry point. If you have a command such as creating an order or a query such as loading a user profile, that logic belongs in a request handler. When the operation does not need to return data, NotifyR still supports the same pattern through void requests, which keeps your application model consistent even for fire-and-forget workflows.

Notifications are a better fit for situations where one action should fan out to multiple listeners. A user being created might trigger audit logging, cache invalidation, and an email notification, and each of those reactions can live in its own handler. That separation keeps the publisher focused on the event itself rather than the consequences of the event.

Pipeline behaviors are the mechanism that lets NotifyR stay composable. Instead of sprinkling timing, validation, or retry logic into each handler, you can wrap the handler pipeline once and apply the same concern consistently across many request types.

## Features

NotifyR is designed around a small set of predictable capabilities:

Request/response handling gives you typed responses without introducing a large abstraction layer.

Void requests make it possible to keep command-style operations consistent even when no data needs to flow back to the caller.

Notifications support one-to-many dispatch, which is useful for events that should trigger multiple side effects.

Pipeline behaviors let you implement cross-cutting logic in one place instead of repeating it in every handler.

Dependency injection is the foundation for registration and resolution, so the library fits naturally into existing ASP.NET Core and generic host applications.

The implementation keeps allocations and reflection to a minimum so that the mediator layer remains a thin adapter instead of a performance bottleneck.

## NuGet

```
dotnet add package NotifyR
```

## Namespace

Most samples use the root namespace directly. That keeps the public surface easy to discover and mirrors the package name.

```csharp
using NotifyR;
```
