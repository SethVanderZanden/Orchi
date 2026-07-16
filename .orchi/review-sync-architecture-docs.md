# Review brief for plan sync-architecture-docs

## Original implementation plan

# Sync backend architecture documentation with current codebase

## Summary
Architecture and pattern docs still describe pre-domain scaffolding (Weather as primary example, “no commands yet”, Agents as “future”). Update them to reflect the live backend: Chats, Agents, Projects, Workspaces, Orchestration, Health.

## Goal
A developer reading `docs/architecture/` and related pattern guides sees accurate project layout, examples, and conventions matching `src/API/` today.

## Scope
- Update architecture README and guides
- Update testing and pattern docs that reference Weather or outdated CQRS state
- Fix incorrect API examples (e.g. `Error.NotFound` signature in result-object doc)
- Follow `.cursor/skills/orchi-documentation/SKILL.md` — Dummy section first on any doc that lacks one or when substantially rewriting

## Out of scope
- Code changes in `src/API/`
- Frontend docs (unless a backend cross-link is stale)
- Creating exhaustive API reference for every endpoint (link to Scalar + `docs/agents/README.md` instead)

## Affected files

### Files to add
None (optional: `docs/backend/README.md` as a short index pointing to architecture + agents docs — only if it improves navigation without duplicating content)

### Files to modify
- `docs/architecture/README.md` — replace Weather-centric layout; list real domains (`Chats/`, `Agents/`, `Projects/`, `Workspaces/`, `Health/`); update “what comes next”
- `docs/architecture/screaming-architecture.md` — replace “future” domain folders with current tree; remove/update `PipelineRun` placeholder note (orchestration uses `OrchestrationWorkflow` entity)
- `docs/architecture/vertical-slice-architecture.md` — use `CreateChat` or `ListProjects` as primary example instead of Weather
- `docs/architecture/cqrs-pipeline.md` — document existing commands (`ICommand`, `ICommandHandler<,>`, `CloseChat` as `ICommand` example); note intentional exceptions (SSE streaming endpoints)
- `docs/architecture/adding-a-feature.md` — point template to `CreateChat.cs` or `CreateProject.cs`; remove “when domain design begins” language; remove Weather removal checklist (done in prior plan)
- `docs/testing/unit-testing.md` — replace Weather examples with a real handler test (e.g. from `tests/Orchi.Api.Tests/Features/Projects/` or integration test pattern)
- `docs/patterns/result-object.md` — fix `Error.NotFound` example to single-argument form matching `Common/Results/Error.cs`
- `docs/patterns/decorator.md` — keep Weather behaviour examples if useful for teaching, but add a note that real slices use the same pipeline; cross-link to `CreateChat` as a command example

### Files to delete
None

## Expected changes

Key doc corrections:

| Doc claim (stale) | Current reality |
|-------------------|-----------------|
| Weather is the sample feature | Real domains exist; Weather removed |
| No command handlers yet | 17+ command handlers across Chats, Agents, Projects, Workspaces |
| `Entities/` dormant | Active entities: `Chat`, `Project`, `Workspace`, `Plan`, `OrchestrationWorkflow`, etc. |
| Agents folder is “future” | `Features/Agents/` has 7 slices |
| `PipelineRun` placeholder | Unused; removed in prior plan |

Document **intentional CQRS exceptions**:
- `SendMessage` — SSE stream; endpoint-only with manual pre-validation
- `SubscribeOrchestrationEvents` — SSE stream
- `GetOrchestration`, `KickOffAll` — thin endpoints over `IOrchestrationWorkflowService` (note as known pattern; plan 5 may refactor)
- `GetHealth` — trivial read, no handler

Document **workspace slice placement**: `CreateWorkspace` lives under `Features/Projects/` (nested resource route) while `UpdateWorkspace`/`DeleteWorkspace` are under `Features/Workspaces/` — explain as route-driven grouping, not inconsistency to fix blindly.

Update project layout tree in README to match:

```
Features/
├── Agents/
├── Chats/
│   └── Orchestration/
├── Projects/
├── Workspaces/
└── Health/
Infrastructure/
├── Agents/       ← agent adapters, orchestration, persistence
├── Caching/
├── Database/
├── Endpoints/
├── OpenApi/
├── Pipeline/
└── Projects/
```

## Tasks
- [ ] Read each affected doc and `.cursor/skills/orchi-documentation/SKILL.md`
- [ ] Update architecture README and four architecture guides
- [ ] Update `unit-testing.md`, `result-object.md`, and `decorator.md`
- [ ] Grep `docs/` for `Weather`, `PipelineRun`, “no command”, “domain design begins” — fix remaining stale text
- [ ] Verify all code links point to existing files
- [ ] Delete this plan file after implementation is complete and validated

## Implementation notes
- `docs/agents/README.md` is largely current — cross-link from architecture README rather than duplicating agent lifecycle
- Dummy sections: add or preserve per skill on docs you substantially rewrite
- Do not change frontend docs unless they reference removed Weather endpoint

## Dependencies and sequencing
Depends on **`remove-scaffold-dead-code`** completing first (Weather/PipelineRun references must match repo state).

Can run in parallel with **`normalize-feature-validation`** and **`align-orchestration-endpoints`**.

## Validation
- Manual read-through of updated docs
- `rg "WeatherForecast|GetWeatherForecast|PipelineRun" docs/` — should return zero or only historical/changelog context
- `rg "no command handlers" docs/` — should return zero
- All markdown links to `src/API/...` paths resolve to existing files

Confirm the plan file has been deleted after successful implementation.

## Handoff notes
This plan is docs-only. If plan 5 refactors orchestration endpoints to full CQRS, update the “intentional exception” note afterward in a follow-up — do not block plan 5 on perfect docs.

Delete `.orchi/plan-sync-architecture-docs.md` when done.

## Implementation chat

Chat ID: `6f7f8254-558e-4413-92c9-5f3820c381b7`

## Parent orchestration chat

Chat ID: `fdad164a-182f-409e-9c66-303bcce002f0`

## Instructions

Review the implementation against the original plan above using the git diff injected into your prompt context.
Produce one or more actionable review plans for the reviewer.