using Microsoft.Extensions.DependencyInjection;
using NotifyR.Tests.Helpers;

namespace NotifyR.Tests;

public class NoBehaviorPipelineTests
{
    [Fact]
    public async Task Send_WithoutBehaviors_InvokesHandlerDirectly()
    {
        using var provider = MediatorBuilder.Build();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new Ping("direct"));

        Assert.Equal("direct", result.Message);
    }
}

public class WithBehaviorPipelineTests
{
    private const string BehaviourKey = "behavior";

    public sealed class Ping2 : IRequest<Pong2>
    {
        public string Message { get; }
        public Ping2(string message) => Message = message;
    }
    public sealed class Pong2;
    public sealed class Ping2Handler : IRequestHandler<Ping2, Pong2>
    {
        public Task<Pong2> Handle(Ping2 request, CancellationToken cancellationToken)
            => Task.FromResult(new Pong2());
    }

    [Fact]
    public async Task Send_WithSingleBehavior_WrapsHandler()
    {
        var order = new List<string>();
        var services = new ServiceCollection();
        services.AddNotifyR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<Ping2Handler>());
        services.AddTransient<IPipelineBehavior<Ping2, Pong2>>(
            _ => new TestBehavior<Ping2, Pong2>(_ => order.Add(BehaviourKey)));
        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new Ping2("wrap"));

        Assert.Equal([BehaviourKey], order);
    }

    [Fact]
    public async Task Send_WithNoBehaviorsInOneProvider_StillResolvesBehaviorsInAnotherProvider()
    {
        using var providerA = new ServiceCollection()
            .AddNotifyR(cfg => cfg.RegisterServicesFromAssemblyContaining<Ping2Handler>())
            .BuildServiceProvider();

        var mediatorA = providerA.GetRequiredService<IMediator>();
        await mediatorA.Send(new Ping2("no-behaviors"));

        var order = new List<string>();
        using var providerB = new ServiceCollection()
            .AddNotifyR(cfg => cfg.RegisterServicesFromAssemblyContaining<Ping2Handler>())
            .AddTransient<IPipelineBehavior<Ping2, Pong2>>(
                _ => new TestBehavior<Ping2, Pong2>(_ => order.Add(BehaviourKey)))
            .BuildServiceProvider();

        var mediatorB = providerB.GetRequiredService<IMediator>();
        await mediatorB.Send(new Ping2("cross-provider"));

        Assert.Equal([BehaviourKey], order);
    }

    [Fact]
    public async Task Send_WithMultipleBehaviors_ExecutesInRegistrationOrder()
    {
        var order = new List<string>();
        var services = new ServiceCollection();
        services.AddNotifyR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<Ping2Handler>());
        services.AddTransient<IPipelineBehavior<Ping2, Pong2>>(
            _ => new TestBehavior<Ping2, Pong2>(_ => order.Add("first")));
        services.AddTransient<IPipelineBehavior<Ping2, Pong2>>(
            _ => new TestBehavior<Ping2, Pong2>(_ => order.Add("second")));
        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new Ping2("order"));

        Assert.Equal(["first", "second"], order);
    }
}
