# CQRS Pipeline

## Dummy section (start here)

Think of every API action as a **form at a service desk**. Some forms are **lookups** ("show me my projects") — read-only. Others are **change requests** ("create a chat") — they alter state.

Orchi separates those mentally (**queries vs commands**) but handles them the same way mechanically: a handler does the work and returns an explicit success or failure ticket (`Result`).

Before the handler runs, automatic **wrappers** check the form (validation), log that someone showed up (logging), and time how long the desk took (performance). The customer (endpoint) only talks to the outermost wrapper — they never pick which checks run.

```
HTTP request  →  Performance wrap  →  Logging wrap  →  Validation wrap  →  Handler  →  Result
```

**The aha:** cross-cutting rules apply everywhere without copy-pasting them into every feature file.

Everything below is the same idea with interfaces, Scrutor, and decorator order.

---

Adapted from [Milan Jovanovic's CQRS without MediatR article](https://www.milanjovanovic.tech/blog/cqrs-pattern-the-way-it-should-have-been-from-the-start).

## Overview

Orchi uses a lightweight CQRS setup — no MediatR, no `ISender`, no runtime dispatching. Handlers are injected directly into endpoints.

Cross-cutting concerns (validation, logging, performance monitoring) are applied with the **decorator pattern** via [Scrutor](https://github.com/khellang/Scrutor). Each behaviour wraps the handler below it and delegates via an `innerHandler` constructor parameter.

For a full explanation of the decorator pattern, DI wiring, and FAQ, see [Decorator Pattern](../patterns/decorator.md).

```
HTTP Request
    ↓
Endpoint (minimal API)
    ↓
PerformanceBehaviour      ← outermost decorator (times full pipeline)
    ↓
LoggingBehaviour          ← logs before/after
    ↓
ValidationBehaviour       ← FluentValidation
    ↓
Handler                   ← business logic (innermost)
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
| `ICommandHandler<TCommand>` | Handles commands without a typed response |
| `ICommandHandler<TCommand, TResponse>` | Handles commands with a typed response |
| `IEndpoint` | Maps a slice to a minimal API route |

## Queries vs commands

**Queries** are reads. There is one handler shape:

| Command/query marker | Handler interface | Returns |
|----------------------|-------------------|---------|
| `IQuery<TResponse>` | `IQueryHandler<TQuery, TResponse>` | `Result<TResponse>` |

**Commands** are writes. They come in two flavours:

| Command marker | Handler interface | Returns | Example use case |
|----------------|-------------------|---------|------------------|
| `ICommand` | `ICommandHandler<TCommand>` | `Result` | Close a chat (success/failure only) |
| `ICommand<TResponse>` | `ICommandHandler<TCommand, TResponse>` | `Result<TResponse>` | Create a chat and return session details |

The codebase has **20+ command handlers** across Chats, Agents, Projects, and Workspaces, plus query handlers like `ListProjects` and `GetChat`. Command and query handlers share the same decorator pipeline.

### Command examples

**With typed response** — [`CreateChat.cs`](../../src/API/Features/Chats/CreateChat/CreateChat.cs):

```csharp
public sealed record Command(...) : ICommand<CreateChatResponse>;

internal sealed class Handler(...) : ICommandHandler<Command, CreateChatResponse> { ... }
```

**Success/failure only** — [`CloseChat.cs`](../../src/API/Features/Chats/CloseChat/CloseChat.cs):

```csharp
public sealed record Command(Guid ChatId) : ICommand;

internal sealed class Handler(...) : ICommandHandler<Command> { ... }
```

## Intentional exceptions

Most slices follow the pattern above. A few endpoints deliberately diverge:

| Slice | Pattern | Why |
|-------|---------|-----|
| [`SendMessage`](../../src/API/Features/Chats/SendMessage/SendMessage.cs) | Handler validates; endpoint streams SSE | Success is a long-lived event stream, not a JSON body |
| [`SubscribeOrchestrationEvents`](../../src/API/Features/Chats/Orchestration/SubscribeEvents/SubscribeOrchestrationEvents.cs) | Handler loads snapshot; endpoint streams SSE | Same — live orchestration events over SSE |
| [`GetOrchestration`](../../src/API/Features/Chats/Orchestration/GetOrchestration/GetOrchestration.cs), [`KickOffAll`](../../src/API/Features/Chats/Orchestration/KickOffAll/KickOffAll.cs) | Thin handlers delegating to `IOrchestrationWorkflowService` | Orchestration logic lives in infrastructure; handlers are adapters |
| [`GetHealth`](../../src/API/Features/Health/GetHealth.cs) | Endpoint only, no handler | Trivial liveness check |

SSE endpoints use the handler for validation and pre-checks, then write directly to `HttpContext.Response` instead of returning `.ToProblem()` on success.

## Result type

All handlers return `Result` or `Result<T>` from `Common/Results/` — explicit success/failure instead of exceptions for expected errors.

```csharp
Result<IReadOnlyList<Response>> result = await handler.Handle(query, ct);
return result.ToProblem();  // Maps to 200 OK or appropriate error response
```

## Behaviours (decorators)

Cross-cutting concerns live in `Common/Behaviours/`. Each behaviour is a static class containing nested decorator types — one per handler interface shape.

### ValidationBehaviour

Runs all registered `IValidator<T>` instances via FluentValidation. Returns `ValidationError` if any rule fails — the endpoint never reaches the handler.

```csharp
internal sealed class QueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> innerHandler,
    IEnumerable<IValidator<TQuery>> validators)
    : IQueryHandler<TQuery, TResponse>
{
    public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
    {
        // validate, then:
        return await innerHandler.Handle(query, cancellationToken);
    }
}
```

### LoggingBehaviour

Logs before and after handler execution. Logs warnings on failure with the error code.

### PerformanceBehaviour

Measures handler execution time with `Stopwatch` and logs elapsed milliseconds.

- **Within threshold** → `LogInformation` with elapsed time
- **Exceeds threshold** → `LogWarning` with elapsed time and configured threshold

Thresholds are configured in `appsettings.json`:

```json
"Performance": {
  "SlowQueryThresholdMs": 500,
  "SlowCommandThresholdMs": 1000
}
```

Options are bound via `PerformanceOptions` in [`PipelineExtensions.cs`](../../src/API/Infrastructure/Pipeline/PipelineExtensions.cs).

### `CommandHandler` vs `CommandBaseHandler`

These are **not** two different domain concepts. Each behaviour defines two nested decorator classes because commands have two handler interfaces:

| Nested decorator class | Wraps | For commands that… |
|------------------------|-------|---------------------|
| `CommandHandler<TCommand, TResponse>` | `ICommandHandler<TCommand, TResponse>` | return a typed response (`ICommand<TResponse>`) |
| `CommandBaseHandler<TCommand>` | `ICommandHandler<TCommand>` | return only success/failure (`ICommand`) |

The name `CommandBaseHandler` means the simpler single-type-parameter handler variant — not a base class to inherit from. Queries only need one decorator shape (`QueryHandler`) because there is only one query handler interface.

### Decorator order

Scrutor applies decorators **last registered = outermost**. The chain in [`PipelineExtensions.cs`](../../src/API/Infrastructure/Pipeline/PipelineExtensions.cs) is:

1. `ValidationBehaviour` (innermost — closest to the real handler)
2. `LoggingBehaviour`
3. `PerformanceBehaviour` (outermost — times validation + handler)

When an endpoint calls `handler.Handle(...)`, execution flows:

```
PerformanceBehaviour.Handle()
  → LoggingBehaviour.Handle()
      → ValidationBehaviour.Handle()
          → ListProjects.Handler.Handle()
```

Each outer layer runs its logic, then calls `innerHandler.Handle(...)` to continue down the chain.

## How DI wiring works

Handlers are registered and decorated in `AddOrchiPipeline`. See the [Decorator Pattern](../patterns/decorator.md) guide for:

- [`AddScoped` vs `Decorate`](../patterns/decorator.md#addscoped-vs-decorate)
- [How `innerHandler` gets injected](../patterns/decorator.md#how-innerhandler-gets-injected)
- [`TryDecorateOpenGeneric`](../patterns/decorator.md#trydecorateopengeneric)
- [FAQ](../patterns/decorator.md#faq)

Summary: assembly scan registers handlers as scoped services; Scrutor `Decorate` wraps each handler with behaviour decorators; the container injects the previous layer as `innerHandler` at resolve time.

## DI registration

Handlers are auto-discovered by assembly scan (excluding `Common.Behaviours` types):

```csharp
services.Scan(scan => scan
    .FromAssemblyOf<Program>()
    .AddClasses(classes => classes
        .AssignableTo(typeof(IQueryHandler<,>))
        .Where(type => type.Namespace?.Contains(".Common.Behaviours") != true), ...)
    .AsImplementedInterfaces()
    .WithScopedLifetime());
```

The same scan registers `ICommandHandler<>` and `ICommandHandler<,>` implementations. Command decorators apply automatically once handlers exist.

## Usage in an endpoint

Handlers are injected directly — no mediator:

```csharp
private static async Task<IResult> Handle(
    IQueryHandler<Query, IReadOnlyList<ProjectSummaryResponse>> handler,
    CancellationToken cancellationToken)
{
    Result<IReadOnlyList<ProjectSummaryResponse>> result =
        await handler.Handle(new Query(), cancellationToken);
    return result.ToProblem();
}
```

The injected `handler` is the outermost decorator. Calling `Handle` runs the full behaviour pipeline before business logic executes.

## Adding a new behaviour

1. Create nested decorator classes in `Common/Behaviours/` — one per handler interface:
   - `QueryHandler<TQuery, TResponse>` for `IQueryHandler<,>`
   - `CommandHandler<TCommand, TResponse>` for `ICommandHandler<,>`
   - `CommandBaseHandler<TCommand>` for `ICommandHandler<>`
2. Accept the matching handler interface as a constructor parameter (`innerHandler`)
3. Implement `Handle` — run your logic, then call `innerHandler.Handle(...)` (unless you short-circuit, like validation on failure)
4. Register with `TryDecorateOpenGeneric` in `PipelineExtensions.cs`
5. Remember: **last registered = outermost**

## Further reading

- [Decorator Pattern](../patterns/decorator.md)
- [Software Patterns](../patterns/README.md)
- [Adding a Feature](adding-a-feature.md)
- [Unit Testing](../testing/unit-testing.md) — testing handlers and behaviours in isolation
