# Request Handling

## `IRequest<TResponse>`

A marker interface that links a request type to its response type. The interface itself is empty — the type parameter exists purely for compile-time type safety.

```csharp
public interface IRequest<TResponse> { }

public class GetUser : IRequest<GetUserResponse>
{
    public int UserId { get; init; }
}
```

## `IRequest` (void)

A convenience shorthand for requests that produce no value:

```csharp
public interface IRequest : IRequest<Unit> { }

public class Ping : IRequest { }
```

Under the hood, void requests are handled as `IRequest<Unit>` where `Unit` is a singleton value type (like `MediatR.Unit`).

## `IRequestHandler<TRequest, TResponse>`

Implement this interface to handle a typed request:

```csharp
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
```

## `IRequestHandler<TRequest>` (void)

Convenience for void handlers — equivalent to `IRequestHandler<TRequest, Unit>`:

```csharp
public interface IRequestHandler<in TRequest> : IRequestHandler<TRequest, Unit>
    where TRequest : IRequest<Unit> { }
```

## How `Send` works

1. `IMediator.Send(request)` is called
2. `ArgumentNullException.ThrowIfNull` guards against null
3. `WrapperCache` looks up or creates a `RequestHandlerWrapperImpl<TRequest, TResponse>` for the request type
4. The wrapper resolves the handler from DI
5. Resolves pipeline behaviors from DI (cached "no behaviors" flag for zero-behavior optimization)
6. Builds the `BehaviorChain` — each behavior wraps the next, innermost is the handler
7. Executes the chain and returns the result

## Multiple requests, different handlers

Each request type gets its own handler. DI resolves the correct `IRequestHandler<TRequest, TResponse>` based on the request type.
