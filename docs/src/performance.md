# Performance

Every allocation on the hot path of `mediator.Send(request)`:

## Warm path (cache populated, 3 behaviors)

```
Send<TResponse>(request, ct)                          ALLOCS
├─ ArgumentNullException.ThrowIfNull                  0
├─ WrapperCache.GetOrCreate → cache hit               0
└─ wrapper.Handle(request, provider, ct)
   ├─ GetRequiredService<IRequestHandler<..>>()       0-N (DI)
   ├─ GetBehaviors(provider)
   │   └─ GetServices<...>().ToArray()                1
   │      (no-behavior flag cached per-provider via CWT)
   ├─ BuildPipeline(handler, behaviors)
   │   ├─ handler.Handle delegate                     1
   │   └─ × N behaviors:
   │       new BehaviorChain                          1 each
   │       chain.Invoke delegate                      1 each
   └─ pipeline(request, ct)                           0
```

| Scenario | Allocations per Send (steady-state) |
|---|---|
| No behaviors | **1** (handler delegate only; no-behavior flag cached per-provider, `GetServices`+`ToArray` skipped) |
| 1 behavior | **4** (handler delegate + `ToArray()` + BehaviorChain + invoke delegate) |
| 3 behaviors | **8** (handler delegate + `ToArray()` + 3×(BehaviorChain + invoke delegate)) |

All are small, short-lived gen-0 objects.

## Cold path (first call per type)

On the first `Send` for a given request type, the wrapper factory runs once:

| Before (Activator.CreateInstance) | After (compiled expression) |
|---|---|
| Reflection-based construction | Cached delegate invocation |

The factory result is cached in `ConcurrentDictionary` — subsequent calls pay zero overhead.

## Publishes with zero handlers

| Before | After |
|---|---|
| `async Task` — class state machine on heap | `async ValueTask` — struct state machine on stack |

When no handlers are registered, the method completes synchronously. `ValueTask` keeps the state machine on the stack.

## What was removed from the hot path

| Removed | Saving |
|---|---|
| `is not TRequest` type check + throw path | 1 branch + 1 dead code path per `Send` |
| Lambda closures per behavior | 1 heap closure per behavior layer |
| `Activator.CreateInstance` reflection | Reflection → delegate call |
| `async Task` class state machine | Heap → stack allocation |
| `Enum.IsDefined` reflection | Range check (JIT-inlinable) |
