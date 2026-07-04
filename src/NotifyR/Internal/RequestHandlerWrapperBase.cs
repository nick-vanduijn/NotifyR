namespace NotifyR;

internal interface IRequestHandlerWrapperBase<TResponse>
{
    Task<TResponse> Handle(
        IRequest<TResponse> request,
        IServiceProvider provider,
        CancellationToken cancellationToken);
}
