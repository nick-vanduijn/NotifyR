# Configuration

## `AddNotifyR`

The entry point for all configuration:

```csharp
services.AddNotifyR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<MyHandler>();
    cfg.UseLifetime(ServiceLifetime.Scoped);
});
```

Must register at least one assembly containing handlers — otherwise an `InvalidOperationException` is thrown.

## `RegisterServicesFromAssembly`

Scan a specific assembly for handlers:

```csharp
cfg.RegisterServicesFromAssembly(typeof(MyHandler).Assembly);
```

Scans all types implementing `IRequestHandler<,>`, `IRequestHandler<>`, `INotificationHandler<>`, or `IPipelineBehavior<,>` (concrete, non-generic types only).

## `RegisterServicesFromAssemblyContaining<T>`

Convenience overload — scans the assembly containing type `T`:

```csharp
cfg.RegisterServicesFromAssemblyContaining<MyHandler>();
```

## `UseLifetime`

Controls how `IMediator` and handlers are registered in DI:

| Lifetime | Behavior |
|---|---|
| `Transient` (default) | New mediator + handlers per resolution |
| `Scoped` | Same mediator + handlers within a scope |
| `Singleton` | **Not allowed** — throws `ArgumentException` |

`Singleton` is rejected because the mediator captures `IServiceProvider`, which becomes the root container when singleton. Handlers resolved through it would ignore their configured scope.

## Assembly scanning details

- Uses `Assembly.GetTypes()` — includes both public and non-public types
- Filters to concrete, non-abstract, non-generic types
- Matches handler interfaces by open generic definition
- Registered with the configured `ServiceLifetime`
- Duplicate assemblies are deduplicated via `.Distinct()` — safe to register the same assembly multiple times
- Reflection type load exceptions are caught gracefully (partial load still works)
