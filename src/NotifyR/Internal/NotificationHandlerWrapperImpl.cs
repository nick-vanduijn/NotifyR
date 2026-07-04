using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace NotifyR;

internal sealed class NotificationHandlerWrapperImpl<TNotification> : INotificationHandlerWrapperBase
    where TNotification : INotification
{
    async ValueTask INotificationHandlerWrapperBase.Handle(
        INotification notification,
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        List<Exception>? exceptions = null;

        foreach (var handler in provider.GetServices<INotificationHandler<TNotification>>())
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await handler.Handle((TNotification)notification, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                (exceptions ??= []).Add(ex);
            }
        }

        if (exceptions is { Count: > 0 })
        {
            if (exceptions.Count is 1)
                ExceptionDispatchInfo.Capture(exceptions[0]).Throw();

            throw new AggregateException(exceptions);
        }
    }
}
