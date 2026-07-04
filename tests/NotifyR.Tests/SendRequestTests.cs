using Microsoft.Extensions.DependencyInjection;
using NotifyR.Tests.Helpers;

namespace NotifyR.Tests;

public class SendRequestTests
{
    [Fact]
    public async Task Send_WithTypedRequest_ReturnsHandlerResponse()
    {
        var mediator = MediatorBuilder.Build().GetRequiredService<IMediator>();

        var result = await mediator.Send(new Ping("hello"));

        Assert.Equal("hello", result.Message);
    }

    [Fact]
    public async Task Send_WithVoidRequest_CompletesSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddNotifyR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<VoidHandler>());
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await mediator.Send(new VoidRequest());
    }

    [Fact]
    public async Task Send_WithNullRequest_ThrowsArgumentNullException()
    {
        var mediator = MediatorBuilder.Build().GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(
            () => mediator.Send((Ping)null!));

        Assert.Equal("request", ex.ParamName);
    }

    [Fact]
    public async Task Send_CancellationTokenForwardsToHandler()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var services = new ServiceCollection();
        services.AddNotifyR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<PingHandler>());
        services.AddTransient<IRequestHandler<Ping, Pong>>(
            _ => new CancellablePingHandler());
        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<OperationCanceledException>(
            () => mediator.Send(new Ping("cancel"), cts.Token));

        Assert.NotNull(ex);
    }
}

public record VoidRequest : IRequest;

public class VoidHandler : IRequestHandler<VoidRequest>
{
    public Task<Unit> Handle(VoidRequest request, CancellationToken cancellationToken)
        => Unit.Completed;
}

public class CancellablePingHandler : IRequestHandler<Ping, Pong>
{
    public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new Pong(request.Message));
    }
}
