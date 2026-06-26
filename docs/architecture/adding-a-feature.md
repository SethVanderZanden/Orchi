# Adding a Feature

Step-by-step checklist for adding a new vertical slice when Orchi domain design begins. Use [`GetWeatherForecast.cs`](../../src/API/Features/Weather/GetForecast/GetWeatherForecast.cs) as the template.

## Checklist

### 1. Create the folder

```
Features/{Domain}/{UseCase}/
└── {UseCase}.cs
```

Example: `Features/Agents/StartAgent/StartAgent.cs`

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

### 3. Implement the handler

```csharp
internal sealed class Handler(/* inject dependencies */)
    : IQueryHandler<Query, Response>  // or ICommandHandler<...>
{
    public async Task<Result<Response>> Handle(Query query, CancellationToken ct)
    {
        // business logic
        return Result.Success(response);
        // or Result.Failure(Error.NotFound("..."));
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

In `tests/Orchi.Api.Tests/Features/{Domain}/`:

**Handler unit test** — test business logic directly:

```csharp
var handler = new YourFeature.Handler(/* mocks or in-memory deps */);
Result<Response> result = await handler.Handle(new YourFeature.Query(...), CancellationToken.None);
Assert.True(result.IsSuccess);
```

**Integration test** — test the HTTP endpoint:

```csharp
HttpResponseMessage response = await _client.GetAsync("/your-route");
Assert.Equal(HttpStatusCode.OK, response.StatusCode);
```

See [Unit Testing](../testing/unit-testing.md) for patterns.

### 7. Verify in Scalar

Run the API and check `http://localhost:5265/scalar/v1` — your endpoint should appear with typed schemas.

## Rules of thumb

- Keep one file per slice unless it grows beyond ~300–400 lines
- Inject shared services (DbContext, external clients) — don't duplicate infrastructure code
- Return `Result<T>` from handlers; map to HTTP in the endpoint via `.ToProblem()`
- Use commands for writes, queries for reads
- Tag endpoints with `.WithTags()` for Scalar grouping

## Removing the Weather sample

When real features replace the sample:

1. Delete `Features/Weather/`
2. Delete `tests/Orchi.Api.Tests/Features/Weather/` and related integration tests
3. Update desktop app if it still references `/WeatherForecast`
