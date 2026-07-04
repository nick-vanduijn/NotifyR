using Microsoft.Extensions.DependencyInjection;
using NotifyR.Tests.Helpers;

namespace NotifyR.Tests;

public class NoBehaviorPipelineTests
{
    [Fact]
    public async Task Send_WithoutBehaviors_InvokesHandlerDirectly()
    {
        var mediator = MediatorBuilder.Build().GetRequiredService<IMediator>();

        var result = await mediator.Send(new Ping("direct"));

        Assert.Equal("direct", result.Message);
    }
}

public class WithBehaviorPipelineTests
{
    private sealed class Ping2 : IRequest<Pong2>
    {
        public string Message { get; }
        public Ping2(string message) => Message = message;
    }
    private sealed class Pong2
    {
        public string Message { get; }
        public Pong2(string message) => Message = message;
    }
    private sealed class Ping2Handler : IRequestHandler<Ping2, Pong2>
    {
        public Task<Pong2> Handle(Ping2 request, CancellationToken cancellationToken)
            => Task.FromResult(new Pong2(request.Message));
    }

    [Fact]
    public async Task Send_WithSingleBehavior_WrapsHandler()
    {
        var order = new List<string>();
        var services = new ServiceCollection();
        services.AddNotifyR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<Ping2Handler>());
        services.AddTransient<IPipelineBehavior<Ping2, Pong2>>(
            _ => new TestBehavior<Ping2, Pong2>(_ => order.Add("behavior")));
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Send(new Ping2("wrap"));

        Assert.Contains("behavior", order);
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
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Send(new Ping2("order"));

        Assert.Equal(["first", "second"], order);
    }
}
