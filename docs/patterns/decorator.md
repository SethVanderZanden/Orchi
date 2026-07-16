# Decorator Pattern

## Dummy section (start here)

Imagine you order a **plain pizza** from a menu. That is the core thing — dough, sauce, cheese. It knows how to be a pizza. It does not know about pepperoni, extra cheese, or a garlic crust.

Now you want extras. You could rewrite the pizza recipe every time ("plain pizza but also add logging pepperoni"). That gets messy fast, and the plain pizza suddenly depends on every topping anyone might want.

The decorator pattern does something simpler: **wrap the plain pizza, add stuff on the way out, hand the customer something that is still a pizza.**

```
You ask for:  "Pizza, please"
                    │
                    ▼
         ┌─────────────────────┐
         │  Garlic crust wrap  │  ← adds something, then passes pizza through
         └──────────┬──────────┘
                    ▼
         ┌─────────────────────┐
         │  Extra cheese wrap  │  ← adds something, then passes pizza through
         └──────────┬──────────┘
                    ▼
         ┌─────────────────────┐
         │  Pepperoni wrap     │  ← adds something, then passes pizza through
         └──────────┬──────────┘
                    ▼
              Plain pizza        ← the actual core (business logic)
```

**Key ideas:**

- **You still ordered "a pizza."** The customer (your endpoint) does not need to know how many wrappers exist. Same menu item, same interface.
- **Each wrapper adds one thing** without rewriting the plain pizza. Logging wrapper logs. Validation wrapper checks the order. Performance wrapper times how long it took.
- **No direct dependency on toppings.** The plain pizza does not import pepperoni. Wrappers sit *around* it. Add or remove wrappers without touching the recipe.
- **Each wrapper passes inward.** Garlic crust does its thing, then hands off to extra cheese, then pepperoni, then plain pizza. That hand-off is `innerHandler` in Orchi code.

**Orchi translation:**

| Pizza world | Orchi world |
|-------------|-------------|
| Plain pizza | `GetWeatherForecast.Handler` (your business logic) |
| Pepperoni wrap | `ValidationBehaviour` (check the order first) |
| Extra cheese wrap | `LoggingBehaviour` (note what happened) |
| Garlic crust wrap | `PerformanceBehaviour` (time how long it took) |
| "One pizza, please" | Endpoint asks for `IQueryHandler<...>` — DI gives the fully wrapped stack |

That is the whole pattern: **decorate the object with extra behaviour without the core object knowing or caring.** Everything below this section is the same idea, but with C#, DI, and Scrutor.

> **Note:** Examples below use a hypothetical `GetWeatherForecast` slice for teaching — the same pipeline wraps every real handler today. For a live command example, see [`CreateChat.cs`](../../src/API/Features/Chats/CreateChat/CreateChat.cs); for a query, see [`ListProjects.cs`](../../src/API/Features/Projects/ListProjects/ListProjects.cs).

---

Orchi uses the [decorator pattern](https://refactoring.guru/design-patterns/decorator) to wrap CQRS handlers with cross-cutting **behaviours** — validation, logging, and performance monitoring — without changing handler or endpoint code.

Implementation: [`Common/Behaviours/`](../../src/API/Common/Behaviours/) + [Scrutor](https://github.com/khellang/Scrutor) `Decorate` in [`PipelineExtensions.cs`](../../src/API/Infrastructure/Pipeline/PipelineExtensions.cs).

See also: [CQRS Pipeline](../architecture/cqrs-pipeline.md) for handler interfaces, command shapes, and endpoint usage.

## What it is

A **decorator** is a wrapper that:

1. Implements the **same interface** as the object it wraps
2. Adds behaviour **before or after** delegating to the inner object
3. Is **transparent** to the caller — the caller still uses the interface

In Orchi, endpoints inject `IQueryHandler<...>` and receive the outermost decorator. They never reference behaviour types directly.

## How Orchi applies it

```
PerformanceBehaviour      ← outermost (times full pipeline)
    ↓
LoggingBehaviour
    ↓
ValidationBehaviour
    ↓
GetWeatherForecast.Handler  ← innermost (business logic)
```

Each behaviour calls `innerHandler.Handle(...)` to continue down the chain. Validation can short-circuit and return early without calling the inner handler.

Current behaviours:

| Behaviour | Purpose |
|-----------|---------|
| `ValidationBehaviour` | Runs FluentValidation before the handler |
| `LoggingBehaviour` | Logs start, completion, and failures |
| `PerformanceBehaviour` | Times execution; warns when slower than configured thresholds |

## Example

```csharp
internal sealed class QueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> innerHandler,
    IEnumerable<IValidator<TQuery>> validators)
    : IQueryHandler<TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
    {
        ValidationFailure[] failures = await ValidateAsync(query, validators, cancellationToken);

        if (failures.Length > 0)
        {
            return Result.Failure<TResponse>(CreateValidationError(failures));
        }

        return await innerHandler.Handle(query, cancellationToken);
    }
}
```

The handler stays focused on business logic. Validation, logging, and timing live in decorators.

## `AddScoped` vs `Decorate`

These both register services in DI, but they do different jobs.

### `AddScoped` (what you are used to)

Registers an interface → implementation mapping:

```csharp
services.AddScoped<
    IQueryHandler<GetWeatherForecast.Query, IReadOnlyList<Response>>,
    GetWeatherForecast.Handler>();
```

Resolve `IQueryHandler<...>` → get `GetWeatherForecast.Handler` directly.

Orchi does this indirectly via assembly scan in `AddOrchiPipeline`.

### `Decorate` (Scrutor)

**Rewrites** an existing registration. The decorator becomes the public implementation; the old implementation is injected into the decorator's constructor:

```csharp
services.Decorate(typeof(IQueryHandler<,>), typeof(ValidationBehaviour.QueryHandler<,>));
```

After decorate:

```
IQueryHandler<Query, Response>
  → ValidationBehaviour.QueryHandler
        └── innerHandler: GetWeatherForecast.Handler
```

Multiple `Decorate` calls stack layers. With Scrutor, **last registered = outermost**.

| Registration | Meaning |
|--------------|---------|
| `AddScoped<I, Impl>()` | "Resolve `Impl` when asked for `I`" |
| `Decorate<I, Wrapper>()` | "Resolve `Wrapper` instead; inject the previous `Impl` into `Wrapper`" |

## How `innerHandler` gets injected

You never call `new ValidationBehaviour.QueryHandler(handler, ...)` in application code. The container builds the chain at resolve time.

**Step 1 — scan registers the real handler:**

```
IQueryHandler<Query, Response>  →  GetWeatherForecast.Handler
```

**Step 2 — `Decorate` replaces that with a wrapper:**

```
IQueryHandler<Query, Response>  →  ValidationBehaviour.QueryHandler
                                      needs IQueryHandler<Query, Response>
                                      → GetWeatherForecast.Handler
```

**Step 3 — at resolve time**, when an endpoint asks for `IQueryHandler<Query, Response>`:

1. Create `GetWeatherForecast.Handler` (innermost)
2. Create `ValidationBehaviour.QueryHandler`, injecting the handler as `innerHandler`
3. Create `LoggingBehaviour.QueryHandler`, injecting validation as `innerHandler`
4. Create `PerformanceBehaviour.QueryHandler`, injecting logging as `innerHandler`
5. Return the performance wrapper to the endpoint

Scrutor finds a constructor parameter on the decorator that matches the decorated interface type and fills it with the previous registration.

### Constructor requirements

The decorator must:

- Implement the same handler interface it wraps
- Accept that interface as a constructor parameter (name does not matter — `innerHandler`, `next`, and `handler` all work)
- Call that parameter's `Handle` method to delegate (unless short-circuiting)

Other parameters (`ILogger`, `IValidator`, `IOptions`) are normal DI — unrelated to the decorate chain.

**Do not** take the concrete handler type (e.g. `GetWeatherForecast.Handler`) in a decorator. That breaks the open-generic pattern and only works for one slice.

## `TryDecorateOpenGeneric`

Orchi wraps `Decorate` with a guard in [`PipelineExtensions.cs`](../../src/API/Infrastructure/Pipeline/PipelineExtensions.cs):

```csharp
private static void TryDecorateOpenGeneric(IServiceCollection services, Type serviceType, Type decoratorType)
{
    if (HasOpenGenericImplementations(services, serviceType))
    {
        services.Decorate(serviceType, decoratorType);
    }
}
```

`HasOpenGenericImplementations` checks whether any closed registration exists for the open generic (e.g. `IQueryHandler<GetWeatherForecast.Query, ...>`).

**Why:** Orchi registers decorators for both queries and commands. `TryDecorateOpenGeneric` skips decoration when no closed handler registration exists yet — useful during startup ordering and when a handler interface has zero implementations. Command handlers (e.g. `CreateChat`, `CloseChat`) are registered and decorated the same way as queries.

## `CommandHandler` vs `CommandBaseHandler`

Each behaviour defines multiple nested decorator classes — not because there are different domain command types, but because C# has two command handler interfaces:

| Nested class | Wraps | Command marker | Returns |
|--------------|-------|----------------|---------|
| `CommandHandler<TCommand, TResponse>` | `ICommandHandler<TCommand, TResponse>` | `ICommand<TResponse>` | `Result<TResponse>` |
| `CommandBaseHandler<TCommand>` | `ICommandHandler<TCommand>` | `ICommand` | `Result` |

`CommandBaseHandler` is naming for the simpler single-type-parameter variant — not a base class to inherit from. Queries only need `QueryHandler` because there is one query handler interface.

## FAQ

Questions distilled from team discussion, with direct answers.

### Are we using the decorator pattern to wrap behaviours around CQRS?

**Yes.** Each behaviour is a decorator that implements the same handler interface as the inner handler. Behaviours run before (and sometimes instead of) the real handler by wrapping it in a chain. Endpoints still inject `IQueryHandler<...>` or `ICommandHandler<...>` — they do not know how many layers exist.

### How does `innerHandler` get passed into `ValidationBehaviour`?

**Via constructor injection, orchestrated by Scrutor.**

`ValidationBehaviour.QueryHandler` declares `IQueryHandler<TQuery, TResponse> innerHandler` in its constructor. When `services.Decorate` runs, Scrutor re-registers the handler so the decorator is returned publicly and the previous implementation is injected into that parameter. At request time, DI constructs each layer inside-out and passes the previous layer as `innerHandler`.

### What does `services.Decorate(serviceType, decoratorType)` actually do?

**It wraps existing DI registrations.**

For every service matching `serviceType`, Scrutor stops resolving the original implementation directly and resolves `decoratorType` instead. The original implementation is injected into a constructor parameter on the decorator that matches the service interface. It builds on top of `AddScoped` — it does not replace lifetime registration; handlers remain scoped per request.

### Do I have to define an `innerHandler` parameter matching the interface type?

**Yes.** The decorator constructor must accept the same interface being decorated. If the parameter is missing or DI cannot resolve it, the app fails at startup when the service provider is built (ASP.NET Core validates the graph in Development). This is intentional — miswired decorators should fail fast, not at first request.

### What is the difference between `CommandHandler` and `CommandBaseHandler` in behaviours?

**They decorate two different handler interfaces.**

- `CommandHandler` — for commands that return a value (`ICommand<TResponse>` → `Result<TResponse>`)
- `CommandBaseHandler` — for commands with no typed return (`ICommand` → `Result`)

Same cross-cutting logic, two shapes because the handler interfaces and return types differ.

### Why does the endpoint not reference behaviour types?

**That is the point of the pattern.** The endpoint depends on the abstraction (`IQueryHandler<...>`). DI supplies the full decorator stack. Adding a new behaviour means registering another `Decorate` call — no endpoint or handler changes required.

## Adding a new behaviour decorator

1. Create nested classes in `Common/Behaviours/` for each handler interface (`QueryHandler`, `CommandHandler`, `CommandBaseHandler`)
2. Accept the matching handler interface as a constructor parameter
3. Implement `Handle` — run your logic, then `return await innerHandler.Handle(...)` unless short-circuiting
4. Register with `TryDecorateOpenGeneric` in `PipelineExtensions.cs`
5. Remember: last registered = outermost

## Testing

Unit tests construct decorators manually, passing the real handler as `innerHandler` — the same wiring Scrutor performs at runtime:

```csharp
var innerHandler = new GetWeatherForecast.Handler();
var behaviour = new ValidationBehaviour.QueryHandler<Query, IReadOnlyList<Response>>(
    innerHandler,
    [new GetWeatherForecast.Validator()]);
```

See [Unit Testing — Behaviour unit tests](../testing/unit-testing.md#behaviour-unit-tests).

## Further reading

- [CQRS Pipeline](../architecture/cqrs-pipeline.md)
- [Scrutor decoration docs](https://github.com/khellang/Scrutor#decoration)
- [Refactoring Guru — Decorator](https://refactoring.guru/design-patterns/decorator)
