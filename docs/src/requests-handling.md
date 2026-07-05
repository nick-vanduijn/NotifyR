# Request Handling

## `IRequest<TResponse>`

`IRequest<TResponse>` is the contract that ties a request type to the value it returns. The interface does not define any members because its purpose is compile-time structure rather than runtime behavior. That design keeps request types lightweight while still making the response type explicit.

```csharp
public interface IRequest<TResponse> { }

public class GetUser : IRequest<GetUserResponse>
{
    public int UserId { get; init; }
}
```

## `IRequest` (void)

`IRequest` is a convenience shorthand for requests that do not produce a value. It still participates in the same pipeline and handler resolution process, which means void operations are not treated as a special case by the consumer.

```csharp
public interface IRequest : IRequest<Unit> { }

public class Ping : IRequest { }
```

Under the hood, void requests are handled as `IRequest<Unit>`. The `Unit` type is a singleton value type that represents the absence of a meaningful return value without introducing `null` or a throwaway object.

## `IRequestHandler<TRequest, TResponse>`

Implement this interface when a request should return a typed result. The handler receives the request and cancellation token and returns the response directly.

```csharp
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
```

## `IRequestHandler<TRequest>` (void)

The void handler form is the same idea with a lighter return contract. It is equivalent to `IRequestHandler<TRequest, Unit>`, which keeps the API readable for operations that only need side effects.

```csharp
public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit>
    where TRequest : IRequest<Unit> { }
```

## How `Send` works

When `IMediator.Send(request)` is called, NotifyR first validates the argument and then locates the wrapper that knows how to handle the request type. That wrapper is cached so the request type only needs to be resolved once. On each call, the wrapper resolves the concrete handler from dependency injection, resolves any registered pipeline behaviors, and then builds the execution chain around the handler.

The pipeline is executed from the outside in. Each behavior gets a chance to inspect the request before delegating to the next step, and the innermost step is always the handler itself. Once the handler completes, the response unwinds back through the behavior chain to the caller.

The important detail is that this orchestration stays generic. Request types do not need to know how handlers are found, and handlers do not need to know how the mediator resolved them. That separation keeps application code focused on the business operation rather than on the plumbing.

## Multiple requests, different handlers

Each request type gets its own handler. Dependency injection resolves the correct `IRequestHandler<TRequest, TResponse>` for the request being sent, so the mediator can remain completely generic while still dispatching to the right implementation.
