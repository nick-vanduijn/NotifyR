# Architecture

## File structure

```
src/NotifyR/
├── Contracts/         ← All public interfaces (IMediator, IRequest, etc.)
├── Extensions/        ← DI configuration (AddNotifyR, NotifyRConfiguration)
├── Internal/          ← Implementation details
│   ├── BehaviorChain.cs                  ← Chain of Responsibility node
│   ├── RequestHandlerWrapperBase.cs      ← Internal interface for request wrappers
│   ├── RequestHandlerWrapperImpl.cs      ← Request pipeline builder + executor
│   ├── RequestHandlerWrapperFactory.cs   ← Compiled expression factory
│   ├── NotificationHandlerWrapperBase.cs ← Internal interface for notification wrappers
│   ├── NotificationHandlerWrapperImpl.cs ← Notification dispatcher
│   ├── NotificationHandlerWrapperFactory.cs ← Compiled expression factory
│   └── WrapperCache.cs                   ← ConcurrentDictionary per-type cache
├── Models/
│   └── Unit.cs          ← Void return type (like MediatR.Unit)
├── Mediator.cs          ← Core IMediator implementation
└── NotifyR.csproj
```

## Namespace

All public types are in the `NotifyR` namespace:

```csharp
using NotifyR;
```

Internal implementation types remain in the same namespace but are `internal`.

## Design decisions

### Why no closure allocations in the pipeline?

Closure allocations for `(req, ct) => behavior.Handle(req, next, ct)` are replaced with an explicit `BehaviorChain` class. Each chain node stores behavior + next delegate as fields rather than capturing them in a closure. This eliminates heap allocations proportional to the number of request types × behaviors per type.

### Why the static "no behaviors" cache?

The `volatile bool s_noBehaviors` in `RequestHandlerWrapperImpl` avoids a `GetServices` DI call + `.ToArray()` allocation on every `Send` when no behaviors are registered. In production, behavior registrations are fixed at startup, so caching the "no behaviors" state is safe.

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
