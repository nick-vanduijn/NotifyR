using Microsoft.Extensions.DependencyInjection;

namespace NotifyR;

internal sealed class RequestHandlerWrapperImpl<TRequest, TResponse> : IRequestHandlerWrapperBase<TResponse>
    where TRequest : IRequest<TResponse>
{
    private static volatile bool s_noBehaviors;

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
        if (s_noBehaviors)
            return [];

        var behaviors = provider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

        if (behaviors.Length is 0)
            s_noBehaviors = true;

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
