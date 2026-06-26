# Vertical Slice Architecture

Adapted from [Milan Jovanovic's VSA guides](https://www.milanjovanovic.tech/blog/vertical-slice-architecture-dotnet).

## What is Vertical Slice Architecture?

Traditional layered architecture scatters one feature across many folders:

```
Controllers/WeatherForecastController.cs
Services/WeatherService.cs
Models/WeatherForecast.cs
Validators/WeatherForecastValidator.cs
```

Vertical Slice Architecture groups everything for one use case in one place:

```
Features/Weather/GetForecast/GetWeatherForecast.cs
```

Each slice is self-contained: request/query, response, handler, validator, and endpoint mapping.

## Why VSA for Orchi?

| Benefit | How it helps |
|---------|--------------|
| **Cohesion** | All code for a feature lives together |
| **Reduced navigation** | No hunting across Controllers/Services/Repositories |
| **Independent evolution** | Simple features stay simple; complex ones can grow |
| **Easier testing** | Handler is a focused unit with clear inputs/outputs |
| **Aligned with business** | Structure matches use cases, not framework layers |

## Orchi's slice structure

Each feature slice is a static class in `Features/{Domain}/{UseCase}/`:

```csharp
public static class GetWeatherForecast
{
    public sealed record Query(int Days = 5) : IQuery<IReadOnlyList<Response>>;
    public sealed record Response(...);

    internal sealed class Handler : IQueryHandler<Query, IReadOnlyList<Response>> { ... }
    public sealed class Validator : AbstractValidator<Query> { ... }
    public sealed class Endpoint : IEndpoint { ... }
}
```

See the live example: [`src/API/Features/Weather/GetForecast/GetWeatherForecast.cs`](../../src/API/Features/Weather/GetForecast/GetWeatherForecast.cs)

## One file vs multiple files

Orchi defaults to **one file per slice** for maximum locality. Split into multiple files when a slice grows beyond ~300–400 lines or has complex validation with many rules.

Both approaches are valid — locality matters more than file count.

## Queries vs commands

- **Query** (`IQuery<TResponse>`) — read-only, no side effects
- **Command** (`ICommand` or `ICommand<TResponse>`) — writes, state changes

The Weather sample is a query. When you add write operations, use commands with `ICommandHandler`.

## Further reading

- [Vertical Slice Architecture Is Easier Than You Think](https://www.milanjovanovic.tech/blog/vertical-slice-architecture-is-easier-than-you-think)
- [Structuring Vertical Slices](https://www.milanjovanovic.tech/blog/vertical-slice-architecture-structuring-vertical-slices)
- [Screaming Architecture](screaming-architecture.md) — how Orchi names its feature folders
- [CQRS Pipeline](cqrs-pipeline.md) — how handlers are invoked and wrapped
