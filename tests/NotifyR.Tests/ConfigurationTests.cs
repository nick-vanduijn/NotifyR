using Microsoft.Extensions.DependencyInjection;
using NotifyR.Tests.Helpers;

namespace NotifyR.Tests;

public class ConfigurationTests
{
    [Fact]
    public void AddNotifyR_ResolvesMediator()
    {
        var services = new ServiceCollection();
        services.AddNotifyR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<PingHandler>());

        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        Assert.IsType<Mediator>(mediator);
    }

    [Fact]
    public void AddNotifyR_ResolvesRegisteredHandlers()
    {
        var services = new ServiceCollection();
        services.AddNotifyR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<PingHandler>());

        using var provider = services.BuildServiceProvider();
        var handler = provider.GetService<IRequestHandler<Ping, Pong>>();

        Assert.IsType<PingHandler>(handler);
    }

    [Fact]
    public void AddNotifyR_ResolvesRegisteredBehaviors()
    {
        var services = new ServiceCollection();
        services.AddNotifyR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<PingHandler>());
        services.AddTransient<IPipelineBehavior<Ping, Pong>>(
            _ => new TestBehavior<Ping, Pong>());

        using var provider = services.BuildServiceProvider();
        var behaviors = provider.GetServices<IPipelineBehavior<Ping, Pong>>().ToArray();

        Assert.Single(behaviors);
    }

    [Fact]
    public void AddNotifyR_WithNullServices_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => ((IServiceCollection)null!).AddNotifyR(cfg => { }));

        Assert.Equal("services", ex.ParamName);
    }

    [Fact]
    public void AddNotifyR_WithNullConfigure_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(
            () => new ServiceCollection().AddNotifyR(null!));

        Assert.Equal("configure", ex.ParamName);
    }

    [Fact]
    public void AddNotifyR_WithNoAssemblies_ThrowsInvalidOperation()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.AddNotifyR(cfg => { }));

        Assert.Equal("No assemblies registered. Call cfg.RegisterServicesFromAssemblyContaining<T>() for at least one assembly that contains your handlers.", ex.Message);
    }

    [Fact]
    public void UseLifetime_Scoped_DoesNotThrow()
    {
        var cfg = new NotifyRConfiguration();
        cfg.RegisterServicesFromAssemblyContaining<PingHandler>();

        var result = cfg.UseLifetime(ServiceLifetime.Scoped);

        Assert.Same(cfg, result);
    }

    [Fact]
    public void UseLifetime_Singleton_ThrowsArgumentException()
    {
        var cfg = new NotifyRConfiguration();
        cfg.RegisterServicesFromAssemblyContaining<PingHandler>();

        var ex = Assert.Throws<ArgumentException>(
            () => cfg.UseLifetime(ServiceLifetime.Singleton));

        Assert.Equal("lifetime", ex.ParamName);
    }

    [Fact]
    public void UseLifetime_InvalidEnumValue_ThrowsArgumentOutOfRange()
    {
        var cfg = new NotifyRConfiguration();
        cfg.RegisterServicesFromAssemblyContaining<PingHandler>();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => cfg.UseLifetime((ServiceLifetime)999));
    }
}
