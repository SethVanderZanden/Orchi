# Orchi Architecture

Orchi follows a **foundation-first** approach: establish architecture, conventions, and testing patterns before building product features. Domain design (agents, worktrees, orchestration) starts only after this foundation is validated.

## Philosophy

- **Vertical Slice Architecture (VSA)** — code organized by feature, not technical layer
- **Screaming Architecture** — folder structure communicates what the system does
- **Custom CQRS pipeline** — commands, queries, and behaviours without MediatR
- **One file per use case** — endpoint, handler, validator, and types live together

The current sample feature is **WeatherForecast** — a familiar scaffold used to demonstrate the patterns. It will be replaced when real Orchi domain work begins.

## Documentation

| Guide | Description |
|-------|-------------|
| [Vertical Slice Architecture](vertical-slice-architecture.md) | What VSA is and how Orchi applies it |
| [Screaming Architecture](screaming-architecture.md) | Domain-first folder structure |
| [CQRS Pipeline](cqrs-pipeline.md) | Commands, queries, and behaviour decorators |
| [Software Patterns](../patterns/README.md) | Design patterns used in Orchi (decorator, result object, DI, etc.) |
| [Adding a Feature](adding-a-feature.md) | Step-by-step checklist for new slices |
| [Unit Testing](../testing/unit-testing.md) | Handler and integration test patterns |
| [Frontend](../frontend/README.md) | TanStack Router and Query (desktop app) |
| [SharedContext](../context/README.md#dummy-section-start-here) | Workspace-scoped collective memory for agents |

## Source references

These guides adapt concepts from [Milan Jovanovic's](https://www.milanjovanovic.tech/blog/vertical-slice-architecture-dotnet) articles on vertical slices, screaming architecture, and CQRS without MediatR.

## Project layout

```
src/API/
├── Common/            Abstractions, behaviours, Result type, HTTP helpers
├── Data/              EF Core DbContext
├── Entities/          Domain entities (dormant until domain design)
├── Features/          Business features — one folder per domain, one file per use case
│   └── Weather/
│       └── GetForecast/
│           └── GetWeatherForecast.cs
├── Infrastructure/    DI extensions (database, pipeline, OpenAPI, endpoints)
└── Program.cs         Thin bootstrap
```

## Quick start

```bash
dotnet run --project src/API          # Opens Scalar at http://localhost:5265/scalar/v1
dotnet test tests/Orchi.Api.Tests     # Run API tests
npm run test:api                      # Same, via npm script
```

## What comes next

When domain design is ready:

1. Create `Features/{Domain}/{UseCase}/{UseCase}.cs`
2. Copy the pattern from `Features/Weather/GetForecast/GetWeatherForecast.cs`
3. Add tests alongside in `tests/Orchi.Api.Tests/Features/{Domain}/`
4. Remove the Weather sample when no longer needed
