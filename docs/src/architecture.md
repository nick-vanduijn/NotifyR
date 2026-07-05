# Architecture

## File structure

The library is organized around a small set of public contracts and a larger internal execution layer. Public interfaces live in `Contracts`, DI configuration lives in `Extensions`, and the actual dispatch mechanics are kept in `Internal` so the surface area stays focused and easy to consume.

```
src/NotifyR/
├── Contracts/
├── Extensions/
├── Internal/
│   ├── BehaviorChain.cs
│   ├── NotificationHandlerWrapperBase.cs
│   ├── NotificationHandlerWrapperFactory.cs
│   ├── NotificationHandlerWrapperImpl.cs
│   ├── RequestHandlerWrapperBase.cs
│   ├── RequestHandlerWrapperFactory.cs
│   ├── RequestHandlerWrapperImpl.cs
│   └── WrapperCache.cs
├── Models/
│   └── Unit.cs
├── Mediator.cs
└── NotifyR.csproj
```

All public types are exposed through the `NotifyR` namespace. That keeps the consumer experience simple and avoids forcing users to learn a deeper namespace hierarchy just to get started.

NotifyR replaces closure-based composition with an explicit `BehaviorChain` class. Instead of creating a lambda that captures the behavior and the next delegate, each chain node stores those values as fields and exposes a concrete `Invoke` method. The main benefit is predictability: the number of allocations no longer grows with the number of request types multiplied by the number of behaviors on the pipeline.

## Namespace

The library declares all its public types in the `NotifyR` namespace (file-scoped):

```csharp
namespace NotifyR;
```

Internal implementation types remain in the same namespace but are `internal`.

## Design decisions

The request path is intentionally direct. A request enters the mediator, the mediator resolves or creates the wrapper for that request type, and the wrapper resolves the handler, resolves any pipeline behaviors, and executes the composed pipeline.

### Why no closure allocations in the pipeline?

Closure allocations for `(req, ct) => behavior.Handle(req, next, ct)` are replaced with an explicit `BehaviorChain` class. Each chain node stores behavior + next delegate as fields rather than capturing them in a closure. This eliminates heap allocations proportional to the number of request types × behaviors per type.

### Why the static "no behaviors" cache?

The `ConditionalWeakTable<object, object>` in `RequestHandlerWrapperImpl` avoids a `GetServices` DI call + `.ToArray()` allocation on every `Send` when no behaviors are registered. The cache is keyed by `IServiceScopeFactory` (or the provider itself when no scope factory is available), so the optimization is shared across all scopes in the same application. In production, behavior registrations are fixed at startup, so caching the "no behaviors" state is safe.

### Why `ValueTask` for notification dispatch?

The `INotificationHandlerWrapperBase` interface uses `ValueTask` instead of `Task`. When no handlers are registered or all handlers complete synchronously, the struct state machine stays on the stack, avoiding a heap allocation.

### Why compiled expression factories?

`Activator.CreateInstance` uses runtime reflection to locate and invoke the constructor. By compiling an `Expression.New` once per closed generic type and caching the delegate, construction becomes a simple delegate call.

## Request flow

```
IMediator.Send(request)
  → Mediator.Send<TResponse>(request)
    → WrapperCache.GetOrCreate(request type)
      → RequestHandlerWrapperFactory.Create (if cache miss)
    → RequestHandlerWrapperImpl.Handle
      → GetRequiredService<IRequestHandler<TRequest, TResponse>>
      → GetBehaviors (no-behavior flag cached per-provider via ConditionalWeakTable)
      → BuildPipeline (BehaviorChain)
      → pipeline(request, ct)
```

## Notification flow

Notifications follow the same broad shape, but instead of building a behavior pipeline they fan out to all registered handlers for the notification type.

```
IMediator.Publish(notification)
  → Mediator.Publish<TNotification>(notification)
    → WrapperCache.GetOrCreate(notification type)
      → NotificationHandlerWrapperFactory.Create (if cache miss)
    → NotificationHandlerWrapperImpl.Handle
      → GetServices<INotificationHandler<TNotification>>
      → foreach handler:
          → handler.Handle(notification, ct)
      → aggregate exceptions if any
```

That split between request flow and notification flow is what keeps the public API simple while still allowing each path to optimize for its own use case. Requests are about returning one result. Notifications are about distributing one fact to many consumers.
