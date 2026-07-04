namespace NotifyR;

internal interface INotificationHandlerWrapperBase
{
    ValueTask Handle(
        INotification notification,
        IServiceProvider provider,
        CancellationToken cancellationToken);
}
