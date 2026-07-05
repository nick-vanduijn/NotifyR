# Pipeline Behaviors

Pipeline behaviors are how NotifyR handles cross-cutting concerns without pushing that logic into every individual handler. The pattern is the same idea used by middleware in ASP.NET Core: each behavior wraps the next behavior, and the final inner step is the handler itself. That makes the pipeline a good place for logging, validation, retry policies, timing, caching, or any other logic that should sit around the request rather than inside it.

## `IPipelineBehavior<TRequest, TResponse>`

Behaviors are generic over both the request and response type so they can participate in the same strongly typed flow as the handler they surround.

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

If three behaviors are registered as A → B → C, the first registered behavior becomes the outermost wrapper. In practice, that means A sees the request first, then B, then C, and the response flows back outward in the reverse order after the handler completes.

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

This ordering matters because it gives the registration list a clear meaning. Behaviors wrap in registration order, execute outer-first, and return inner-first, which is the same mental model most .NET developers already know from middleware.

## Short-circuiting

A behavior can skip the `next()` call entirely to short-circuit the pipeline. That is useful when a cached result is available, when validation fails early, or when a policy decides the request should not continue.

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

The pipeline is assembled by `RequestHandlerWrapperImpl.BuildPipeline`. The handler sits at the center of the chain, and each behavior is wrapped around it from the inside out so the resulting delegate can be invoked as one composed operation.

```csharp
pipeline = handler.Handle;                              // innermost
for (var i = behaviors.Length - 1; i >= 0; i--)
    pipeline = new BehaviorChain(behaviors[i], pipeline).Invoke;  // wrap outward
```

`behaviors` is the array returned by `provider.GetServices<IPipelineBehavior<TRequest, TResponse>>()` in DI registration order (`behaviors[0]` is the first registered). By iterating from last to first, the pipeline is composed so the first-registered behavior ends up as the outermost wrapper, matching the execution order documented above.

Each `BehaviorChain` node stores two things: the behavior instance and a delegate to the next step in the chain. That explicit shape keeps the implementation easy to reason about while avoiding the hidden allocations that closures would introduce.

No closures are allocated. Each node is an explicit class with stored fields, which is a deliberate tradeoff in favor of predictable allocation behavior on the hot path.

## No-behavior cache (per-root-container)

When no behaviors are registered for a request type, NotifyR caches that fact using a `ConditionalWeakTable`. The cache is keyed by `IServiceScopeFactory`, which is effectively a singleton for the root container, so the optimization is shared across all scopes in the same application. If the provider does not expose `IServiceScopeFactory`, the provider itself becomes the key.

On later calls, the cache hit avoids the `GetServices` and `ToArray` work entirely. That matters because the no-behavior case is common in smaller applications and in request types that do not need cross-cutting logic.

When one or more behaviors are registered, they are still resolved from dependency injection on every `Send`. NotifyR does not cache the resolved behavior instances, because doing so would interfere with the configured lifetime semantics. Transient behaviors should be created per call, scoped behaviors should live for the current scope, and singleton behaviors should be created once.

## Registration

Behaviors can be registered as open generics or as closed types, depending on whether they should apply broadly or only to a specific request pair.

```csharp
// Open generic — applies to all requests
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

// Closed generic — applies to specific request type only
services.AddTransient<IPipelineBehavior<Ping, Pong>, MyPingBehavior>();
```
