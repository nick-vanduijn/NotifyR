using Microsoft.Extensions.DependencyInjection;

namespace NotifyR.Tests.Helpers;

internal static class MediatorBuilder
{
    internal static ServiceProvider Build(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddNotifyR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<PingHandler>());

        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }
}
