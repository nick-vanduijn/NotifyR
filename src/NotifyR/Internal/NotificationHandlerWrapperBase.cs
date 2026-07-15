namespace NotifyR;

internal interface INotificationHandlerWrapperBase
{
    Task Handle(
        INotification notification,
        IServiceProvider provider,
        CancellationToken cancellationToken);
}
