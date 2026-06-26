# CQRS Pipeline

Adapted from [Milan Jovanovic's CQRS without MediatR article](https://www.milanjovanovic.tech/blog/cqrs-pattern-the-way-it-should-have-been-from-the-start).

## Overview

Orchi uses a lightweight CQRS setup — no MediatR, no `ISender`, no runtime dispatching. Handlers are injected directly into endpoints.

```
HTTP Request
    ↓
Endpoint (minimal API)
    ↓
LoggingBehaviour          ← outermost decorator
    ↓
ValidationBehaviour       ← FluentValidation
    ↓
Handler                   ← business logic
    ↓
Result<T>                 ← explicit success/failure
```

## Abstractions

Located in `src/API/Common/Abstractions/`:

| Interface | Purpose |
|-----------|---------|
| `IQuery<TResponse>` | Marker for read operations |
| `ICommand` / `ICommand<TResponse>` | Marker for write operations |
| `IQueryHandler<TQuery, TResponse>` | Handles queries, returns `Result<TResponse>` |
| `ICommandHandler<TCommand>` | Handles commands without response |
| `ICommandHandler<TCommand, TResponse>` | Handles commands with response |
| `IEndpoint` | Maps a slice to a minimal API route |

## Result type

All handlers return `Result` or `Result<T>` from `Common/Results/` — explicit success/failure instead of exceptions for expected errors.

```csharp
Result<IReadOnlyList<Response>> result = await handler.Handle(query, ct);
return result.ToProblem();  // Maps to 200 OK or appropriate error response
```

## Behaviours (decorators)

Cross-cutting concerns wrap handlers via [Scrutor](https://github.com/khellang/Scrutor) decorators in `Common/Behaviours/`:

### ValidationBehaviour

Runs all registered `IValidator<T>` instances via FluentValidation. Returns `ValidationError` if any rule fails — the endpoint never reaches the handler.

### LoggingBehaviour

Logs before and after handler execution. Logs warnings on failure with the error code.

### Decorator order

Decorators are applied last-to-first (outermost runs first):

1. LoggingBehaviour (outermost — sees everything)
2. ValidationBehaviour
3. Handler (innermost)

Registration is in [`Infrastructure/Pipeline/PipelineExtensions.cs`](../../src/API/Infrastructure/Pipeline/PipelineExtensions.cs).

## DI registration

Handlers are auto-discovered by assembly scan (excluding `Common.Behaviours` types):

```csharp
services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes
        .AssignableTo(typeof(IQueryHandler<,>))
        .Where(type => type.Namespace?.Contains(".Common.Behaviours") != true), ...)
    ...
);
```

Command handler registration is included even though no commands exist yet — ready for when write operations are added.

## Usage in an endpoint

Handlers are injected directly — no mediator:

```csharp
private static async Task<IResult> Handle(
    [AsParameters] Query query,
    IQueryHandler<Query, IReadOnlyList<Response>> handler,
    CancellationToken cancellationToken)
{
    Result<IReadOnlyList<Response>> result = await handler.Handle(query, cancellationToken);
    return result.ToProblem();
}
```

## Adding a new behaviour

1. Create a decorator class in `Common/Behaviours/` implementing the handler interface
2. Register with `TryDecorateOpenGeneric` in `PipelineExtensions.cs`
3. Place it in the decorator chain (remember: last registered = outermost)

## Further reading

- [Adding a Feature](adding-a-feature.md)
- [Unit Testing](../testing/unit-testing.md) — testing handlers and behaviours in isolation
