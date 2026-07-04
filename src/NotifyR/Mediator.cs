namespace NotifyR;

public sealed class Mediator(IServiceProvider provider) : IMediator
{
    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var wrapper = WrapperCache<IRequestHandlerWrapperBase<TResponse>>
            .GetOrCreate(request.GetType(), RequestHandlerWrapperFactory.Create<TResponse>);

        return wrapper.Handle(request, provider, cancellationToken);
    }

    public Task Send(IRequest request, CancellationToken cancellationToken = default) =>
        Send<Unit>(request, cancellationToken);

    public Task Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var wrapper = WrapperCache<INotificationHandlerWrapperBase>
            .GetOrCreate(notification.GetType(), NotificationHandlerWrapperFactory.Create);

        return wrapper.Handle(notification, provider, cancellationToken).AsTask();
    }
}
