using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace NotifyR;

internal static class NotificationHandlerWrapperFactory
{
    private static readonly ConcurrentDictionary<Type, Func<object>> s_factories = new();

    internal static INotificationHandlerWrapperBase Create(Type notificationType)
    {
        var closedType = typeof(NotificationHandlerWrapperImpl<>)
            .MakeGenericType(notificationType);

        var factory = s_factories.GetOrAdd(closedType, static t =>
            Expression.Lambda<Func<object>>(Expression.New(t)).Compile());

        return (INotificationHandlerWrapperBase)factory();
    }
}
