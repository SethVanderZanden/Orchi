# Screaming Architecture

Adapted from [Milan Jovanovic's Screaming Architecture article](https://www.milanjovanovic.tech/blog/screaming-architecture).

## The idea

When you look at a project's folder structure, can you tell what the system **does**?

A structure organized by technical concerns screams "ASP.NET Core":

```
Controllers/
Services/
Repositories/
Models/
```

A structure organized by use cases screams the **business domain**:

```
Features/
└── Weather/
    └── GetForecast/
```

Orchi uses the latter. As real features are added, the tree will scream "AI engineering orchestrator" — not "web framework."

## Use-case driven folders

Each top-level folder under `Features/` represents a domain concept:

```
Features/
├── Weather/          ← sample (temporary)
│   └── GetForecast/
├── Agents/           ← future
├── Worktrees/        ← future
└── Reviews/          ← future
```

Inside each domain folder, one subfolder per use case:

```
Features/Weather/
└── GetForecast/
    └── GetWeatherForecast.cs
```

## What stays outside Features/

Not everything belongs in a slice. Shared infrastructure lives separately:

| Folder | Purpose |
|--------|---------|
| `Common/` | CQRS abstractions, behaviours, Result type |
| `Infrastructure/` | DI setup, OpenAPI, endpoint discovery |
| `Data/` | EF Core DbContext |
| `Entities/` | Shared domain entities (when domain design begins) |

Shared logic used by multiple slices (email, storage, etc.) goes in `Common/` or a dedicated module — not duplicated across slices.

## Benefits

- **High cohesion** within a use case
- **Low coupling** between unrelated use cases
- **Easier navigation** for new developers
- **Aligned with business language** — folders match how stakeholders describe the system

## When domain design begins

The `Weather/` folder is a temporary sample. When Orchi's real domain is designed:

1. Add new domain folders under `Features/`
2. Leave `Entities/` for shared domain models used across slices
3. Remove `Weather/` when the sample is no longer needed

The `PipelineRun` entity in `Entities/` is an early placeholder — dormant until orchestration domain design starts.

## Further reading

- [Vertical Slice Architecture](vertical-slice-architecture.md)
- [Adding a Feature](adding-a-feature.md)
