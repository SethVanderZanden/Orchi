# Dependency Injection

Orchi uses ASP.NET Core's built-in DI container with constructor injection throughout the API.

## Where it appears

| Area | How DI is used |
|------|----------------|
| **Handlers** | Auto-registered by assembly scan (`AddOrchiPipeline`) |
| **Behaviours** | Registered as Scrutor decorators wrapping handlers |
| **Endpoints** | Handler interfaces injected into minimal API route handlers |
| **Infrastructure** | `DbContext`, validators, loggers, options resolved by the container |
| **Lifetimes** | Handlers and the full decorator chain are **scoped** (per HTTP request) |

## Registration flow

`Program.cs`:

```csharp
builder.Services
    .AddOrchiDatabase(builder.Configuration)
    .AddOrchiPipeline(builder.Configuration)
    .AddOrchiOpenApi();
```

`AddOrchiPipeline` scans for handlers, registers FluentValidation validators, and applies behaviour decorators.

## Constructor injection

Dependencies are declared in primary constructors or explicit constructors — never resolved manually with `new` for services:

```csharp
internal sealed class Handler(AppDbContext db) : IQueryHandler<Query, Response>
{
    public async Task<Result<Response>> Handle(Query query, CancellationToken ct) { ... }
}
```

Behaviour decorators receive the inner handler the same way — see [Decorator — How innerHandler gets injected](decorator.md#how-innerhandler-gets-injected).

## Related

- [Decorator](decorator.md) — Scrutor `Decorate` extends DI with wrappers
- [CQRS Pipeline](../architecture/cqrs-pipeline.md#di-registration)
