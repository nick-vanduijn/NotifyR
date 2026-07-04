namespace NotifyR;

internal sealed class BehaviorChain<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IPipelineBehavior<TRequest, TResponse> _behavior;
    private readonly RequestHandlerDelegate<TRequest, TResponse> _next;

    internal BehaviorChain(
        IPipelineBehavior<TRequest, TResponse> behavior,
        RequestHandlerDelegate<TRequest, TResponse> next)
    {
        _behavior = behavior;
        _next = next;
    }

    internal Task<TResponse> Invoke(TRequest request, CancellationToken cancellationToken) =>
        _behavior.Handle(request, _next, cancellationToken);
}
