using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;

namespace NotifyR;

internal sealed class NotificationHandlerWrapperImpl<TNotification> : INotificationHandlerWrapperBase
    where TNotification : INotification
{
    async Task INotificationHandlerWrapperBase.Handle(
        INotification notification,
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var options = provider.GetRequiredService<NotifyROptions>();
        var handlers = provider.GetServices<INotificationHandler<TNotification>>().ToArray();

        if (handlers.Length is 0)
        {
            if (options.ThrowOnNoHandlers)
                throw new InvalidOperationException(
                    $"No notification handler registered for {typeof(TNotification).Name}.");

            return;
        }

        List<Exception>? exceptions = null;

        foreach (var handler in handlers)
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
