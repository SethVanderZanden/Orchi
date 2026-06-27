# Unit Testing

Orchi API tests live in [`tests/Orchi.Api.Tests/`](../../tests/Orchi.Api.Tests/).

## Running tests

```bash
dotnet test tests/Orchi.Api.Tests
# or
npm run test:api
```

## Test structure

```
tests/Orchi.Api.Tests/
├── Common/
│   ├── CollectingLogger.cs            # Captures log entries in behaviour tests
│   └── TestWebApplicationFactory.cs   # WebApplicationFactory<Program>
├── Features/
│   └── Weather/
│       └── GetWeatherForecastTests.cs # Handler + behaviour unit tests
└── Integration/
    └── WeatherForecastEndpointTests.cs # Full HTTP pipeline tests
```

## Handler unit tests

Test business logic in isolation — no HTTP, no DI container:

```csharp
var handler = new GetWeatherForecast.Handler();
Result<IReadOnlyList<GetWeatherForecast.Response>> result =
    await handler.Handle(new GetWeatherForecast.Query(Days: 3), CancellationToken.None);

Assert.True(result.IsSuccess);
Assert.Equal(3, result.Value.Count);
```

Handlers are `internal` — the API project exposes them to tests via `InternalsVisibleTo` in `Orchi.Api.csproj`.

## Behaviour unit tests

Behaviour unit tests construct decorators manually — the same way Scrutor wires them at runtime, but without the DI container. Pass the real handler as `innerHandler`:

```csharp
var innerHandler = new GetWeatherForecast.Handler();
var validator = new GetWeatherForecast.Validator();
var behaviour = new ValidationBehaviour.QueryHandler<Query, IReadOnlyList<Response>>(
    innerHandler, [validator]);

Result<IReadOnlyList<Response>> result =
    await behaviour.Handle(new Query(Days: 99), CancellationToken.None);

Assert.True(result.IsFailure);
Assert.IsType<ValidationError>(result.Error);
```

To assert logging output, use `CollectingLogger<T>` from `tests/Orchi.Api.Tests/Common/`:

```csharp
var logger = new CollectingLogger<IQueryHandler<Query, IReadOnlyList<Response>>>();
var behaviour = new PerformanceBehaviour.QueryHandler<Query, IReadOnlyList<Response>>(
    innerHandler,
    logger,
    Options.Create(new PerformanceOptions { SlowQueryThresholdMs = 10 }));

await behaviour.Handle(new Query(Days: 1), CancellationToken.None);

Assert.Contains(logger.Entries, entry =>
    entry.Level == LogLevel.Warning &&
    entry.Message.Contains("Slow Query Query"));
```

In production, Scrutor injects `innerHandler` automatically — see [Decorator Pattern](../patterns/decorator.md#how-innerhandler-gets-injected).

## Integration tests

Test the full HTTP pipeline via `WebApplicationFactory<Program>`:

```csharp
public class WeatherForecastEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public WeatherForecastEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetWeatherForecast_ReturnsOkWithForecasts()
    {
        HttpResponseMessage response = await _client.GetAsync("/WeatherForecast");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
```

Integration tests exercise the real DI container, behaviour pipeline, and endpoint mapping.

## When adding real features

For each new slice, add:

1. **Handler tests** in `Features/{Domain}/{UseCase}Tests.cs`
2. **Integration tests** in `Integration/{Domain}EndpointTests.cs` (or co-located with handler tests for small features)

For database-dependent handlers, use EF Core InMemory provider in unit tests or rely on integration tests with `WebApplicationFactory`.

## Further reading

- [Adding a Feature](../architecture/adding-a-feature.md)
- [Decorator Pattern](../patterns/decorator.md)
- [CQRS Pipeline](../architecture/cqrs-pipeline.md)
