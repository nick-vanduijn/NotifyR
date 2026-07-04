using Microsoft.Extensions.DependencyInjection;
using NotifyR.Tests.Helpers;

namespace NotifyR.Tests;

public class PublishNotificationTests
{
    private sealed class HandlerA(NotificationTracker tracker) : INotificationHandler<MyNotification>
    {
        public Task Handle(MyNotification notification, CancellationToken cancellationToken)
        {
            tracker.Calls.Add(("A", notification.Value));
            return Task.CompletedTask;
        }
    }

    private sealed class HandlerB(NotificationTracker tracker) : INotificationHandler<MyNotification>
    {
        public Task Handle(MyNotification notification, CancellationToken cancellationToken)
        {
            tracker.Calls.Add(("B", notification.Value));
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler(string message) : INotificationHandler<MyNotification>
    {
        public Task Handle(MyNotification notification, CancellationToken cancellationToken)
            => throw new InvalidOperationException(message);
    }

    [Fact]
    public async Task Publish_CallsSingleHandler()
    {
        var tracker = new NotificationTracker();
        var mediator = MediatorBuilder.Build(services =>
        {
            services.AddSingleton(tracker);
            services.AddTransient<INotificationHandler<MyNotification>, HandlerA>();
        }).GetRequiredService<IMediator>();

        await mediator.Publish(new MyNotification("test"));

        var call = Assert.Single(tracker.Calls);
        Assert.Equal("A", call.HandlerId);
        Assert.Equal("test", call.Value);
    }

    [Fact]
    public async Task Publish_CallsAllHandlers()
    {
        var tracker = new NotificationTracker();
        var mediator = MediatorBuilder.Build(services =>
        {
            services.AddSingleton(tracker);
            services.AddTransient<INotificationHandler<MyNotification>, HandlerA>();
            services.AddTransient<INotificationHandler<MyNotification>, HandlerB>();
        }).GetRequiredService<IMediator>();

        await mediator.Publish(new MyNotification("multi"));

        Assert.Equal(2, tracker.Calls.Count);
        Assert.Equal("A", tracker.Calls[0].HandlerId);
        Assert.Equal("B", tracker.Calls[1].HandlerId);
    }

    [Fact]
    public async Task Publish_WithNullNotification_ThrowsArgumentNullException()
    {
        var mediator = MediatorBuilder.Build().GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<ArgumentNullException>(
            () => mediator.Publish((MyNotification)null!));

        Assert.Equal("notification", ex.ParamName);
    }

    [Fact]
    public async Task Publish_WithSingleHandlerException_Rethrows()
    {
        var mediator = MediatorBuilder.Build(services =>
        {
            services.AddTransient<INotificationHandler<MyNotification>>(
                _ => new ThrowingHandler("fail"));
        }).GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Publish(new MyNotification("x")));

        Assert.Equal("fail", ex.Message);
    }

    [Fact]
    public async Task Publish_WithMultipleHandlerExceptions_ThrowsAggregateException()
    {
        var mediator = MediatorBuilder.Build(services =>
        {
            services.AddTransient<INotificationHandler<MyNotification>>(
                _ => new ThrowingHandler("e1"));
            services.AddTransient<INotificationHandler<MyNotification>>(
                _ => new ThrowingHandler("e2"));
        }).GetRequiredService<IMediator>();

        var ex = await Assert.ThrowsAsync<AggregateException>(
            () => mediator.Publish(new MyNotification("x")));

        Assert.Equal(2, ex.InnerExceptions.Count);
    }
}
