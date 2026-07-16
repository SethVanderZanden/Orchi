# Result Object

## Dummy section (start here)

When you order food, the kitchen does not **throw a plate at you** when they're out of salmon — they hand you a ticket that says "sold out, try the chicken." Expected outcomes should be **normal return values**, not surprises that crash the program.

Orchi handlers return `Result` or `Result<T>` — a small envelope that is either **success with a value** or **failure with a structured error**. Endpoints translate that envelope to HTTP (200, 404, 400, etc.) via `.ToProblem()`.

```
Handler  →  Result.Success(data)     →  endpoint  →  200 OK + JSON
         →  Result.Failure(error)   →  endpoint  →  404/400 + problem details
```

**The aha:** control flow stays explicit; validation and not-found paths do not rely on exceptions.

Everything below is the same idea with types and code.

---

Orchi handlers return `Result` or `Result<T>` from `Common/Results/` instead of throwing exceptions for **expected** failures (validation errors, not-found, conflicts).

## Where it appears

- All `IQueryHandler` and `ICommandHandler` implementations
- Mapped to HTTP responses via `.ToProblem()` in endpoints

```csharp
public async Task<Result> Handle(Command command, CancellationToken ct)
{
    if (entity is null)
    {
        return Result.Failure(Error.NotFound($"Project '{command.ProjectId}' was not found."));
    }

    return Result.Success();
}
```

## Why use it

| Benefit | Detail |
|---------|--------|
| Explicit control flow | Success and failure are visible in the return type |
| No exception overhead | Expected paths do not use try/catch |
| Pipeline-friendly | Behaviours can inspect `result.IsSuccess` without catching |
| HTTP mapping | `ToProblem()` converts errors to appropriate status codes |

## Types

| Type | Use when |
|------|----------|
| `Result` | Command with no typed response |
| `Result<T>` | Query or command returning a value |
| `ValidationError` | FluentValidation failures from `ValidationBehaviour` |
| `Error` | Structured error with code and message |

### `Error` helpers

Defined in [`Error.cs`](../../src/API/Common/Results/Error.cs):

| Method | Signature | Notes |
|--------|-----------|-------|
| `Error.NotFound` | `NotFound(string message)` | Code is always `"NotFound"` |
| `Error.Validation` | `Validation(string code, string message)` | Custom code for domain validation |

## Related

- [CQRS Pipeline](../architecture/cqrs-pipeline.md#result-type)
- [Adding a Feature](../architecture/adding-a-feature.md)
