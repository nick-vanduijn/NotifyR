using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace NotifyR;

internal static class RequestHandlerWrapperFactory
{
    private static readonly ConcurrentDictionary<Type, Func<object>> s_factories = new();

    internal static IRequestHandlerWrapperBase<TResponse> Create<TResponse>(Type requestType)
    {
        var closedType = typeof(RequestHandlerWrapperImpl<,>)
            .MakeGenericType(requestType, typeof(TResponse));

        var factory = s_factories.GetOrAdd(closedType, static t =>
            Expression.Lambda<Func<object>>(Expression.New(t)).Compile());

        return (IRequestHandlerWrapperBase<TResponse>)factory();
    }
}
