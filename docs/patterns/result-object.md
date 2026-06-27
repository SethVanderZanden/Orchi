# Result Object

Orchi handlers return `Result` or `Result<T>` from `Common/Results/` instead of throwing exceptions for **expected** failures (validation errors, not-found, conflicts).

## Where it appears

- All `IQueryHandler` and `ICommandHandler` implementations
- Mapped to HTTP responses via `.ToProblem()` in endpoints

```csharp
public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
{
    if (entity is null)
    {
        return Result.Failure<Response>(Error.NotFound("Entity.NotFound", "Not found."));
    }

    return Result.Success(response);
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

## Related

- [CQRS Pipeline](../architecture/cqrs-pipeline.md#result-type)
- [Adding a Feature](../architecture/adding-a-feature.md)
