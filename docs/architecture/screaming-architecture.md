# Screaming Architecture

## Dummy section (start here)

Open two restaurant floor plans. **Plan A** labels rooms "Heat source," "Cutting surface," "Storage." **Plan B** labels them "Grill station," "Salad bar," "Dessert counter." Plan B tells you what gets *served*; Plan A only tells you what *equipment* exists.

**Screaming architecture** means your codebase layout should read like Plan B — a new developer glances at folders and immediately knows the product: chats, projects, agents, orchestration.

```
Technical layout (screams "web framework")     vs     Orchi layout (screams "AI orchestrator")

Controllers/                                          Features/
Services/                                                 ├── Chats/
Repositories/                                             ├── Projects/
Models/                                                   ├── Agents/
                                                          └── Workspaces/
```

**The aha:** structure is documentation — names should match how you talk about the product, not how ASP.NET is organized.

Everything below is the same idea with Orchi's actual folder tree.

---

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
├── Chats/
│   └── CreateChat/
└── Projects/
    └── ListProjects/
```

Orchi uses the latter. The tree screams "AI engineering orchestrator" — not "web framework."

## Use-case driven folders

Each top-level folder under `Features/` represents a domain concept:

```
Features/
├── Agents/           ← agent model catalog, mode defaults
├── Chats/            ← chat lifecycle, messaging, plans
│   └── Orchestration/  ← multi-agent plan orchestration
├── Projects/         ← projects and nested workspace creation
├── Workspaces/       ← workspace update/delete
└── Health/           ← liveness probe
```

Inside each domain folder, one subfolder per use case:

```
Features/Chats/
├── CreateChat/
│   └── CreateChat.cs
├── ListChats/
│   └── ListChats.cs
└── Orchestration/
    └── GetOrchestration/
        └── GetOrchestration.cs
```

### Route-driven grouping

Not every slice lives under the folder you'd guess from the resource name alone. **`CreateWorkspace`** sits under `Features/Projects/` because its route is nested under a project (`POST /projects/{projectId}/workspaces`). **`UpdateWorkspace`** and **`DeleteWorkspace`** live under `Features/Workspaces/` because their routes are top-level (`PATCH/DELETE /workspaces/{id}`).

This is intentional **route-driven grouping**, not a mistake to "fix" by moving files. Group slices by the URL shape they serve.

## What stays outside Features/

Not everything belongs in a slice. Shared infrastructure lives separately:

| Folder | Purpose |
|--------|---------|
| `Common/` | CQRS abstractions, behaviours, Result type |
| `Infrastructure/` | DI setup, OpenAPI, endpoint discovery, agent adapters |
| `Data/` | EF Core DbContext |
| `Entities/` | Shared domain entities used across slices |

Active entities include `Chat`, `Project`, `Workspace`, `Plan`, `OrchestrationWorkflow`, `AgentModel`, and related types. Orchestration state is persisted via `OrchestrationWorkflow` — not a separate placeholder entity.

Shared logic used by multiple slices (caching, agent adapters, project store) goes in `Infrastructure/` or `Common/` — not duplicated across slices.

## Benefits

- **High cohesion** within a use case
- **Low coupling** between unrelated use cases
- **Easier navigation** for new developers
- **Aligned with business language** — folders match how stakeholders describe the system

## Further reading

- [Vertical Slice Architecture](vertical-slice-architecture.md)
- [Adding a Feature](adding-a-feature.md)
- [Agent adapters](../agents/README.md)
