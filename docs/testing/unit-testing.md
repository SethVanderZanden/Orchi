# Unit Testing

## Dummy section (start here)

Testing Orchi's API is like checking a restaurant at two zoom levels:

1. **Recipe test** — give the chef ingredients, taste the dish. No front door, no wait staff. That's a **handler unit test**.
2. **Full service test** — walk in the front door, place an order, get a plate back. That's an **integration test** through HTTP.

Both matter. Recipe tests are fast and pinpoint logic bugs. Full-service tests catch wiring mistakes (wrong route, missing DI registration, broken pipeline).

```
Handler unit test     →  new Handler(deps).Handle(...)     →  assert Result
Integration test      →  HttpClient.PostAsync("/projects") →  assert status + body
```

Everything below is the same idea with test project layout and examples.

---

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
│   ├── ProjectTestHelper.cs           # Shared project/workspace setup
│   └── TestWebApplicationFactory.cs   # WebApplicationFactory<Program>
├── Features/
│   └── Projects/
│       └── ProjectsEndpointTests.cs   # Project CRUD integration tests
└── Integration/
    ├── ChatsEndpointTests.cs          # Chat lifecycle HTTP tests
    ├── CreateChatModeModelDefaultTests.cs
    └── ...                            # Other endpoint integration tests
```

Infrastructure-heavy logic (parsers, stores, orchestration steps) also has tests under `Infrastructure/`.

## Handler unit tests

Test business logic in isolation — no HTTP, no DI container:

```csharp
var handler = new CloseChat.Handler(sessionManager);
Result result = await handler.Handle(new CloseChat.Command(chatId), CancellationToken.None);

Assert.True(result.IsSuccess);
```

Handlers are `internal` — the API project exposes them to tests via `InternalsVisibleTo` in `Orchi.Api.csproj`.

Inject real in-memory dependencies or test doubles depending on what the handler needs. Database-dependent handlers often rely on integration tests instead.

## Behaviour unit tests

Behaviour unit tests construct decorators manually — the same way Scrutor wires them at runtime, but without the DI container. Pass the real handler as `innerHandler`:

```csharp
var innerHandler = new CreateChat.Handler(sessionManager);
var validator = new CreateChat.Validator();
var behaviour = new ValidationBehaviour.CommandHandler<CreateChat.Command, CreateChatResponse>(
    innerHandler, [validator]);

Result<CreateChatResponse> result =
    await behaviour.Handle(new CreateChat.Command("", Guid.Empty, null, null), CancellationToken.None);

Assert.True(result.IsFailure);
Assert.IsType<ValidationError>(result.Error);
```

To assert logging output, use `CollectingLogger<T>` from `tests/Orchi.Api.Tests/Common/`:

```csharp
var logger = new CollectingLogger<IQueryHandler<ListProjects.Query, IReadOnlyList<ProjectSummaryResponse>>>();
var behaviour = new PerformanceBehaviour.QueryHandler<ListProjects.Query, IReadOnlyList<ProjectSummaryResponse>>(
    innerHandler,
    logger,
    Options.Create(new PerformanceOptions { SlowQueryThresholdMs = 10 }));

await behaviour.Handle(new ListProjects.Query(), CancellationToken.None);

Assert.Contains(logger.Entries, entry =>
    entry.Level == LogLevel.Warning &&
    entry.Message.Contains("Slow Query Query"));
```

In production, Scrutor injects `innerHandler` automatically — see [Decorator Pattern](../patterns/decorator.md#how-innerhandler-gets-injected).

## Integration tests

Test the full HTTP pipeline via `WebApplicationFactory<Program>`:

```csharp
public class ProjectsEndpointTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly HttpClient _client;

    public ProjectsEndpointTests(TestWebApplicationFactory factory)
    {
        factory.InitializeDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProject_ReturnsProjectWithDefaultWorkspace()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/projects",
            new CreateProjectRequest("Orchi", Directory.GetCurrentDirectory()));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CreateProjectResponse? created = await response.Content.ReadFromJsonAsync<CreateProjectResponse>();
        Assert.NotNull(created);
        Assert.Equal("Orchi", created.Name);
    }
}
```

See the full suite in [`ProjectsEndpointTests.cs`](../../tests/Orchi.Api.Tests/Features/Projects/ProjectsEndpointTests.cs).

Integration tests exercise the real DI container, behaviour pipeline, and endpoint mapping.

## When adding new features

For each new slice, add:

1. **Handler tests** in `Features/{Domain}/{UseCase}Tests.cs` when handler logic is non-trivial
2. **Integration tests** in `Integration/{Domain}EndpointTests.cs` or co-located under `Features/{Domain}/`

For database-dependent handlers, use EF Core InMemory provider in unit tests or rely on integration tests with `WebApplicationFactory`.

## Further reading

- [Adding a Feature](../architecture/adding-a-feature.md)
- [Decorator Pattern](../patterns/decorator.md)
- [CQRS Pipeline](../architecture/cqrs-pipeline.md)
