namespace NotifyR;

public delegate Task<TResponse> RequestHandlerDelegate<in TRequest, TResponse>(
    TRequest request,
    CancellationToken cancellationToken);
