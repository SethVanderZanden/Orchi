# Options Pattern

Orchi binds configuration sections to strongly typed options classes using `IOptions<T>`.

## Where it appears

`PerformanceOptions` controls slow-query and slow-command thresholds for `PerformanceBehaviour`:

```json
"Performance": {
  "SlowQueryThresholdMs": 500,
  "SlowCommandThresholdMs": 1000
}
```

```csharp
internal sealed class PerformanceOptions
{
    public const string SectionName = "Performance";

    public int SlowQueryThresholdMs { get; init; } = 500;
    public int SlowCommandThresholdMs { get; init; } = 1000;
}
```

Registered in `AddOrchiPipeline`:

```csharp
services.Configure<PerformanceOptions>(configuration.GetSection(PerformanceOptions.SectionName));
```

Injected into behaviours:

```csharp
internal sealed class QueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> innerHandler,
    ILogger<...> logger,
    IOptions<PerformanceOptions> options)
```

## Why use it

- Configuration stays in `appsettings.json` / environment variables
- Typesafe access with defaults in the options class
- Easy to override per environment (`appsettings.Development.json`)

## Related

- [Decorator — PerformanceBehaviour](decorator.md)
- [CQRS Pipeline](../architecture/cqrs-pipeline.md)
