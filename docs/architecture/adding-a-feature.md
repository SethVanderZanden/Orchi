# Adding a Feature

## Dummy section (start here)

Adding a backend feature in Orchi is like **adding a new item to a menu**. You write one recipe card (the slice file), hang it in the right section of the kitchen (`Features/{Domain}/`), and the restaurant's existing systems — order window discovery, quality checks, timing — pick it up automatically.

You do not register routes by hand or wire validation separately unless the feature is special (like streaming SSE).

**Checklist in plain terms:** create folder → define request → write handler → optional validator → map endpoint → write tests → verify in Scalar.

Everything below is the same checklist with code templates.

---

Step-by-step checklist for adding a new vertical slice. Use [`CreateChat.cs`](../../src/API/Features/Chats/CreateChat/CreateChat.cs) for a command template or [`CreateProject.cs`](../../src/API/Features/Projects/CreateProject/CreateProject.cs) for a project-scoped command with persistence.

## Checklist

### 1. Create the folder

```
Features/{Domain}/{UseCase}/
└── {UseCase}.cs
```

Example: `Features/Agents/AddAgentModel/AddAgentModel.cs`

### 2. Define the request and response

For reads:

```csharp
public sealed record Query(/* parameters */) : IQuery<Response>;
public sealed record Response(/* fields */);
```

For writes:

```csharp
public sealed record Command(/* parameters */) : ICommand<Response>;
// or : ICommand for no response
public sealed record Response(/* fields */);
```

| Marker | Handler to implement | Returns |
|--------|---------------------|---------|
| `ICommand` | `ICommandHandler<Command>` | `Result` |
| `ICommand<TResponse>` | `ICommandHandler<Command, TResponse>` | `Result<TResponse>` |

Behaviours (validation, logging, performance) wrap command handlers automatically via the same decorator pipeline as queries. See [Decorator Pattern](../patterns/decorator.md).

### 3. Implement the handler

```csharp
internal sealed class Handler(/* inject dependencies */)
    : IQueryHandler<Query, Response>  // or ICommandHandler<...>
{
    public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
    {
        // business logic
        return Result.Success(response);
        // or Result.Failure(Error.NotFound("Not found."));
    }
}
```

### 4. Add validation (optional)

```csharp
public sealed class Validator : AbstractValidator<Query>
{
    public Validator()
    {
        RuleFor(q => q.SomeField).NotEmpty();
    }
}
```

Validators are auto-discovered by FluentValidation. The ValidationBehaviour runs them automatically.

### 5. Map the endpoint

```csharp
public sealed class Endpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/your-route", Handle)   // or MapPost, MapPut, etc.
            .WithName("YourEndpointName")
            .WithTags("YourDomain")
            .Produces<Response>();
    }

    private static async Task<IResult> Handle(
        [AsParameters] Query query,
        IQueryHandler<Query, Response> handler,
        CancellationToken ct) =>
        (await handler.Handle(query, ct)).ToProblem();
}
```

The `IEndpoint` implementation is auto-discovered by [`EndpointExtensions.cs`](../../src/API/Infrastructure/Endpoints/EndpointExtensions.cs) — no manual registration needed.

### 6. Write tests

In `tests/Orchi.Api.Tests/Features/{Domain}/` or `Integration/`:

**Handler unit test** — test business logic directly:

```csharp
var handler = new YourFeature.Handler(/* mocks or in-memory deps */);
Result<Response> result = await handler.Handle(new YourFeature.Query(...), CancellationToken.None);
Assert.True(result.IsSuccess);
```

**Integration test** — test the HTTP endpoint (see [`ProjectsEndpointTests.cs`](../../tests/Orchi.Api.Tests/Features/Projects/ProjectsEndpointTests.cs)):

```csharp
HttpResponseMessage response = await _client.PostAsJsonAsync("/projects", new CreateProjectRequest(...));
Assert.Equal(HttpStatusCode.Created, response.StatusCode);
```

See [Unit Testing](../testing/unit-testing.md) for patterns.

### 7. Verify in Scalar

Run the API and check `http://localhost:5265/scalar/v1` — your endpoint should appear with typed schemas.

## Rules of thumb

- Keep one file per slice unless it grows beyond ~300–400 lines
- Inject shared services (DbContext, stores, `AgentSessionManager`) — don't duplicate infrastructure code
- Return `Result<T>` from handlers; map to HTTP in the endpoint via `.ToProblem()` (unless streaming SSE)
- Use commands for writes, queries for reads
- Tag endpoints with `.WithTags()` for Scalar grouping
- Place nested-resource routes under the parent domain folder when it aids navigation (see [Screaming Architecture — route-driven grouping](screaming-architecture.md#route-driven-grouping))

## SSE streaming slices

If the response is a long-lived **Server-Sent Events** stream, follow [`SendMessage.cs`](../../src/API/Features/Chats/SendMessage/SendMessage.cs): use the handler for validation and context, then write events from the endpoint. See [CQRS Pipeline — intentional exceptions](cqrs-pipeline.md#intentional-exceptions).
