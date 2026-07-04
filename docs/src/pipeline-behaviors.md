# Pipeline Behaviors

Pipeline behaviors implement cross-cutting concerns using the **Chain of Responsibility** pattern. Each behavior wraps the next, forming a middleware stack around the request handler.

## `IPipelineBehavior<TRequest, TResponse>`

```csharp
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken);
}
```

## Execution order

With three behaviors registered as A → B → C:

```
Request
  │
  ▼
BehaviorA.Handle(req, next, ct)
  │
  ├─ BehaviorA does pre-work (logging, validation, etc.)
  │
  ├─ next(req, ct)
  │    │
  │    ▼
  │   BehaviorB.Handle(req, next, ct)
  │    │
  │    ├─ BehaviorB does pre-work
  │    ├─ next(req, ct)
  │    │    │
  │    │    ▼
  │    │   BehaviorC.Handle(req, next, ct)
  │    │    │
  │    │    ├─ BehaviorC does pre-work
  │    │    ├─ next(req, ct) → handler.Handle(req, ct)
  │    │    ├─ BehaviorC does post-work
  │    │    └─ returns response
  │    │
  │    ├─ BehaviorB does post-work
  │    └─ returns response
  │
  ├─ BehaviorA does post-work
  └─ returns response
```

**Behaviors wrap in registration order, execute outer-first, return inner-first** — identical to ASP.NET Core middleware.

## Short-circuiting

A behavior can skip the `next()` call entirely to short-circuit the pipeline:

```csharp
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TRequest, TResponse> next,
        CancellationToken ct)
    {
        var cached = TryGetFromCache(request);
        if (cached is not null)
            return cached;            // ← skips next(), returns early

        return await next(request, ct);
    }
}
```

## Internal design

The pipeline is constructed by `RequestHandlerWrapperImpl.BuildPipeline`:

```csharp
pipeline = handler.Handle;                              // innermost
for (var i = behaviors.Length - 1; i >= 0; i--)
    pipeline = new BehaviorChain(behaviors[i], pipeline).Invoke;  // wrap outward
```

Each `BehaviorChain` node stores:

- A reference to the `IPipelineBehavior<TRequest, TResponse>` instance
- A delegate to the next node's `Invoke` method

No closures are allocated — each node is an explicit class with stored fields.

## No-behavior optimization

When zero behaviors are registered for a request type, a `volatile bool` is set to `true` on first call. Subsequent calls skip the DI `GetServices` call entirely, avoiding the array allocation.

## Registration

Behaviors can be registered as open generics or closed types:

```csharp
// Open generic — applies to all requests
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// Closed generic — applies to specific request type only
services.AddTransient<IPipelineBehavior<Ping, Pong>, MyPingBehavior>();
```
