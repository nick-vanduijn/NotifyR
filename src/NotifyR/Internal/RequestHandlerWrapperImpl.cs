using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace NotifyR;

internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : IRequestHandlerWrapperBase<TResponse>
    where TRequest : IRequest<TResponse>
{
    private static readonly ConditionalWeakTable<object, object> s_noBehaviorsCache = new();
    private static readonly object s_sentinel = new();
    private static readonly ConditionalWeakTable<object, object>.CreateValueCallback s_addSentinel =
        static _ => s_sentinel;

    Task<TResponse> IRequestHandlerWrapperBase<TResponse>.Handle(
        IRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken cancellationToken)
    {
        var handler = provider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = GetBehaviors(provider);
        var pipeline = BuildPipeline(handler, behaviors);

        return pipeline((TRequest)request, cancellationToken);
    }

    private static IPipelineBehavior<TRequest, TResponse>[] GetBehaviors(IServiceProvider provider)
    {
        var scopeFactory = provider.GetService<IServiceScopeFactory>();
        var key = scopeFactory ?? (object)provider;

        if (s_noBehaviorsCache.TryGetValue(key, out _))
            return [];

        var behaviors = provider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

        if (behaviors.Length is 0)
            s_noBehaviorsCache.GetValue(key, s_addSentinel);

        return behaviors;
    }

    private static RequestHandlerDelegate<TRequest, TResponse> BuildPipeline(
        IRequestHandler<TRequest, TResponse> handler,
        IPipelineBehavior<TRequest, TResponse>[] behaviors)
    {
        RequestHandlerDelegate<TRequest, TResponse> pipeline = handler.Handle;

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var chain = new BehaviorChain<TRequest, TResponse>(behaviors[i], pipeline);
            pipeline = chain.Invoke;
        }

        return pipeline;
    }
}
