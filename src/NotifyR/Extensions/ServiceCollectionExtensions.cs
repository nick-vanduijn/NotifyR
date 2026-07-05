using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NotifyR;

public static class ServiceCollectionExtensions
{
    private static void ValidateArguments(
        IServiceCollection services,
        Action<NotifyRConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
    }

    public static IServiceCollection AddNotifyR(
        this IServiceCollection services,
        Action<NotifyRConfiguration> configure)
    {
        ValidateArguments(services, configure);

        var config = new NotifyRConfiguration();
        configure(config);

        ValidateConfiguration(config);
        RegisterMediatorService(services, config);
        RegisterAssemblyHandlers(services, config);

        return services;
    }

    private static void ValidateConfiguration(NotifyRConfiguration config)
    {
        if (config.AssembliesToScan.Count is not 0)
        {
            return;
        }

        throw new InvalidOperationException(
            "No assemblies registered. "
            + "Call cfg.RegisterServicesFromAssemblyContaining<T>() "
            + "for at least one assembly that contains your handlers.");
    }

    private static void RegisterMediatorService(
        IServiceCollection services,
        NotifyRConfiguration config)
    {
        services.Add(new ServiceDescriptor(typeof(IMediator), typeof(Mediator), config.Lifetime));
    }

    private static void RegisterAssemblyHandlers(
        IServiceCollection services,
        NotifyRConfiguration config)
    {
        foreach (var assembly in config.AssembliesToScan.Distinct())
        {
            RegisterTypesFromAssembly(services, assembly, config.Lifetime);
        }
    }

    private static void RegisterTypesFromAssembly(
        IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime)
    {
        var types = LoadTypes(assembly);

        foreach (var type in types.Where(IsConcreteType))
        {
            RegisterHandlerInterfaces(services, type, lifetime);
        }
    }

    private static IEnumerable<Type> LoadTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    private static bool IsConcreteType(Type type) =>
        type is { IsAbstract: false, IsInterface: false, IsGenericTypeDefinition: false };

    private static void RegisterHandlerInterfaces(
        IServiceCollection services,
        Type type,
        ServiceLifetime lifetime)
    {
        var handlerInterfaces = type.GetInterfaces()
            .Where(IsHandlerInterface);

        foreach (var handlerInterface in handlerInterfaces)
        {
            services.Add(new ServiceDescriptor(handlerInterface, type, lifetime));
        }
    }

    private static bool IsHandlerInterface(Type interfaceType)
    {
        if (!interfaceType.IsGenericType)
            return false;

        var openGeneric = interfaceType.GetGenericTypeDefinition();

        return openGeneric == typeof(IRequestHandler<,>)
            || openGeneric == typeof(INotificationHandler<>)
            || openGeneric == typeof(IPipelineBehavior<,>);
    }
}
