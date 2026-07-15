using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NotifyR;

public sealed class NotifyRConfiguration
{
    internal List<Assembly> AssembliesToScan { get; } = [];
    internal ServiceLifetime Lifetime { get; private set; } = ServiceLifetime.Transient;
    internal bool ThrowOnNoHandlers { get; private set; }

    public NotifyRConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        AssembliesToScan.Add(assembly);

        return this;
    }

    public NotifyRConfiguration RegisterServicesFromAssemblyContaining<T>() =>
        RegisterServicesFromAssembly(typeof(T).Assembly);

    public NotifyRConfiguration UseLifetime(ServiceLifetime lifetime)
    {
        if (lifetime is < ServiceLifetime.Singleton or > ServiceLifetime.Transient)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime));
        }

        if (lifetime is ServiceLifetime.Singleton)
        {
            throw new ArgumentException(
                "Singleton is not supported for the mediator. "
                + "The mediator captures IServiceProvider, which becomes the root container "
                + "when Singleton — handlers resolved through it ignore their configured scope. "
                + "Use Transient (default) or Scoped instead.", nameof(lifetime));
        }

        Lifetime = lifetime;

        return this;
    }

    public NotifyRConfiguration SetThrowOnNoHandlers(bool throwOnNoHandlers = true)
    {
        ThrowOnNoHandlers = throwOnNoHandlers;

        return this;
    }
}
