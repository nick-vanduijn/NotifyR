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

## No-behavior cache (per-root-container)

When zero behaviors are registered for a given request type, a sentinel is cached using a `ConditionalWeakTable`. The cache key is `IServiceScopeFactory` (a singleton per root DI container), so the optimization is shared across all scopes within the same container. If the container does not expose `IServiceScopeFactory`, the provider itself is used as the key.

On subsequent calls, the cache hit avoids the `GetServices`+`ToArray` call entirely.

When one or more behaviors are registered, they are resolved from DI on every `Send` — no caching of resolved instances occurs. This preserves correct DI lifetime semantics: transient behaviors are created fresh per call, scoped behaviors live for the scope, and singleton behaviors are created once.

## Registration

Behaviors can be registered as open generics or closed types:

```csharp
// Open generic — applies to all requests
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// Closed generic — applies to specific request type only
services.AddTransient<IPipelineBehavior<Ping, Pong>, MyPingBehavior>();
```
