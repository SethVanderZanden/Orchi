# Orchi Architecture

## Dummy section (start here)

Imagine Orchi's backend as a **restaurant organized by dish**, not by job title. You do not hunt through a "Knives folder" and a "Ovens folder" to find how burgers are made — you go to the **Burgers** section and everything for that dish lives there: recipe, plating, and the window where orders go out.

Orchi's API is built the same way. Each business action (create a chat, list projects, kick off a plan) is one **vertical slice** — one file with its request, handler, validator, and HTTP route. Shared plumbing (database, logging, validation pipeline) sits outside the slices so features stay readable.

```
Customer order  →  Front window (endpoint)  →  Kitchen (handler)  →  Plate (Result)
                         ↑
              Validation / logging / timing wrap the kitchen automatically
```

**Orchi translation:**

| Analogy | Orchi |
|---------|-------|
| Dish section (Burgers, Salads) | `Features/{Domain}/` — Chats, Projects, Agents, … |
| One recipe card per dish | One `.cs` file per use case (`CreateChat.cs`, `ListProjects.cs`) |
| Shared kitchen equipment | `Infrastructure/` — database, agents, caching, pipeline |
| "Order accepted" or "sold out" ticket | `Result<T>` instead of throwing for expected errors |

**The aha:** folder names tell you *what the app does* (chats, projects, orchestration), not *which framework layer* you are in.

Everything below is the same idea with C#, DI, and file paths.

---

Orchi follows **Vertical Slice Architecture** with a custom CQRS pipeline — commands, queries, and behaviour decorators without MediatR. The backend is live product code: chats, agent adapters, projects, workspaces, and orchestration — not a scaffold.

## Philosophy

- **Vertical Slice Architecture (VSA)** — code organized by feature, not technical layer
- **Screaming Architecture** — folder structure communicates what the system does
- **Custom CQRS pipeline** — commands, queries, and behaviours without MediatR
- **One file per use case** — endpoint, handler, validator, and types live together

## Documentation

| Guide | Description |
|-------|-------------|
| [Vertical Slice Architecture](vertical-slice-architecture.md#dummy-section-start-here) | What VSA is and how Orchi applies it |
| [Screaming Architecture](screaming-architecture.md#dummy-section-start-here) | Domain-first folder structure |
| [CQRS Pipeline](cqrs-pipeline.md#dummy-section-start-here) | Commands, queries, and behaviour decorators |
| [Software Patterns](../patterns/README.md) | Design patterns used in Orchi (decorator, result object, DI, etc.) |
| [Adding a Feature](adding-a-feature.md#dummy-section-start-here) | Step-by-step checklist for new slices |
| [Unit Testing](../testing/unit-testing.md#dummy-section-start-here) | Handler and integration test patterns |
| [Frontend](../frontend/README.md) | TanStack Router and Query (desktop app) |
| [Agent adapters](../agents/README.md) | Chat persistence, Cursor CLI integration, and agent lifecycle |

## Source references

These guides adapt concepts from [Milan Jovanovic's](https://www.milanjovanovic.tech/blog/vertical-slice-architecture-dotnet) articles on vertical slices, screaming architecture, and CQRS without MediatR.

## Project layout

```
src/API/
├── Common/            Abstractions, behaviours, Result type, HTTP helpers
├── Data/              EF Core DbContext
├── Entities/          Shared domain entities (Chat, Project, Workspace, Plan, OrchestrationWorkflow, …)
├── Features/          Business features — one folder per domain, one file per use case
│   ├── Agents/
│   ├── Chats/
│   │   └── Orchestration/
│   ├── Projects/
│   ├── Workspaces/
│   └── Health/
├── Infrastructure/    DI extensions and adapters
│   ├── Agents/        Agent adapters, orchestration, chat/plan persistence
│   ├── Caching/
│   ├── Database/
│   ├── Endpoints/
│   ├── OpenApi/
│   ├── Pipeline/
│   └── Projects/
└── Program.cs         Thin bootstrap
```

Browse live endpoints in Scalar after starting the API. For agent-specific behaviour (adapters, prompts, streaming), see [Agent adapters](../agents/README.md).

## Quick start

```bash
dotnet run --project src/API          # Opens Scalar at http://localhost:5265/scalar/v1
dotnet test tests/Orchi.Api.Tests     # Run API tests
npm run test:api                      # Same, via npm script
```

## What comes next

When adding a new capability:

1. Create `Features/{Domain}/{UseCase}/{UseCase}.cs`
2. Copy the pattern from [`CreateChat.cs`](../../src/API/Features/Chats/CreateChat/CreateChat.cs) (command) or [`ListProjects.cs`](../../src/API/Features/Projects/ListProjects/ListProjects.cs) (query)
3. Add tests in `tests/Orchi.Api.Tests/Features/{Domain}/` or `Integration/`
4. Follow [Adding a Feature](adding-a-feature.md) for the full checklist
