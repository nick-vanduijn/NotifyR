# Configuration

## `AddNotifyR`

`AddNotifyR` is the entry point for configuring NotifyR in the service collection. It is where you tell the library which assemblies should be scanned and which lifetime should be used for the mediator and handlers.

```csharp
services.AddNotifyR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<MyHandler>();
    cfg.UseLifetime(ServiceLifetime.Scoped);
});
```

At least one assembly containing handlers must be registered. If no assemblies are provided, NotifyR throws an `InvalidOperationException` because it has nothing to discover or wire up.

## `RegisterServicesFromAssembly`

This method scans a specific assembly for handlers. It is useful when your handlers live in a dedicated assembly or when you want to keep configuration explicit.

```csharp
cfg.RegisterServicesFromAssembly(typeof(MyHandler).Assembly);
```

NotifyR scans concrete, non-generic types that implement `IRequestHandler<,>`, `IRequestHandler<>`, `INotificationHandler<>`, or `IPipelineBehavior<,>`. That keeps registration focused on application code instead of abstract base types or helper classes that should not be activated directly.

## `RegisterServicesFromAssemblyContaining<T>`

This is a convenience overload for the common case where you already have a type in the target assembly. It keeps startup code short without hiding what is being scanned.

```csharp
cfg.RegisterServicesFromAssemblyContaining<MyHandler>();
```

## `UseLifetime`

`UseLifetime` controls how `IMediator` and the discovered handlers are registered. The default is transient, which gives each resolution a new mediator instance and new handlers unless the underlying services use a broader lifetime.

| Lifetime | Behavior |
|---|---|
| `Transient` (default) | New mediator + handlers per resolution |
| `Scoped` | Same mediator + handlers within a scope |
| `Singleton` | **Not allowed** — throws `ArgumentException` |

`Singleton` is rejected because the mediator captures `IServiceProvider`. If the mediator itself were singleton, handler resolution would happen through the root container and scoped dependencies would lose their expected lifetime boundaries.

## Assembly scanning details

Assembly scanning uses `Assembly.GetTypes()`, which means both public and non-public types can be discovered. NotifyR then filters to concrete, non-abstract, non-generic types and matches handler interfaces by open generic definition before registering the results with the configured lifetime.

Duplicate assemblies are deduplicated with `.Distinct()`, so it is safe to call the registration helpers more than once with the same assembly. Type load exceptions are handled gracefully as well, which means a partial scan can still succeed if one type fails to load.
