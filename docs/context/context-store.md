# Context Store

## Dummy section (start here)

The context store is the team's **filing cabinet** — one drawer per workspace, with folders for files, symbols, and session notes. Every chat in the same repo opens the same drawer.

| Analogy | Orchi |
|---------|-------|
| Drawer label | Normalized `WorkspacePath` |
| File cards | `sc_IndexedFiles` |
| Name index | `sc_Symbols` |
| Meeting notes | `sc_TaskSummaries` (session distillations) |

---

## Status

**done** — see [PROGRESS.md](PROGRESS.md)

## Interface

`IContextStore` in [`src/SharedContext/Storage/IContextStore.cs`](../../src/SharedContext/Storage/IContextStore.cs)

SQLite database: `orchi-context.db` (configurable via `SharedContext:ConnectionString`).

## Tables (Phase 1)

| Table | Purpose |
|-------|---------|
| `sc_Workspaces` | Workspace metadata, last indexed, git info |
| `sc_IndexedFiles` | Path, hash, language, summary |
| `sc_Symbols` | Name, kind, line range |
| `sc_TaskSummaries` | Per-chat rolling session summary |

## Session distiller

`ISessionDistiller` appends user/assistant turn bullets to the chat's `TaskSummary` after each completed turn. Injected into prompts via `IPromptBuilder` as `## Session summary`.

Implementation: [`src/SharedContext/Session/SessionDistiller.cs`](../../src/SharedContext/Session/SessionDistiller.cs)

## Deferred

- `ArchitectureDoc`, `Decision`, `AgentNote`, `BuildTestRun` entities (Phase 4).
- PostgreSQL backend for team environments.
