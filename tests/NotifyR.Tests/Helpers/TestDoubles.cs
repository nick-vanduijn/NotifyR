namespace NotifyR.Tests.Helpers;

public class Ping : IRequest<Pong>
{
    public string Message { get; }
    public Ping(string message) => Message = message;
}

public class Pong
{
    public string Message { get; }
    public Pong(string message) => Message = message;
}

public class PingHandler : IRequestHandler<Ping, Pong>
{
    public Task<Pong> Handle(Ping request, CancellationToken cancellationToken)
        => Task.FromResult(new Pong(request.Message));
}

public class MyNotification : INotification
{
    public string Value { get; }
    public MyNotification(string value) => Value = value;
}

public class NotificationTracker
{
    public List<(string HandlerId, string Value)> Calls { get; } = [];
}

public class TestBehavior<TRequest, TResponse>(
    Action<string>? onInvoke = null)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TRequest, TResponse> next,
        CancellationToken cancellationToken)
    {
        onInvoke?.Invoke(typeof(TRequest).Name);
        return next(request, cancellationToken);
    }
}
