# SharedContext

## Dummy section (start here)

Imagine a software team with one **shared notebook** for the whole project. Every developer writes discoveries there — what each file does, decisions made, what changed in git — instead of each person re-reading the entire codebase from scratch.

Orchi.SharedContext is that notebook for AI agents.

| Analogy | Orchi |
|---------|-------|
| Shared notebook | `IContextStore` (SQLite) |
| Librarian who updates the notebook | `IProjectIndexer` |
| Finding the right page | `IVectorStore` / keyword search |
| What goes in the agent's letterhead | `IPromptBuilder` stable prefix |
| What goes in today's letter | `IPromptBuilder` dynamic context |
| Office announcements | `IWorkspaceEventBus` |

**The aha:** One workspace-scoped memory layer feeds every chat, so agents stop duplicating repo scans and mode switches keep context.

Everything below is the same idea with file paths, components, and integration points.

---

## Overview

`Orchi.SharedContext` lives in [`src/SharedContext/`](../src/SharedContext/). The API registers it via `AddOrchiSharedContext()` in [`Program.cs`](../src/API/Program.cs).

### Components

| Component | Doc |
|-----------|-----|
| Project Indexer | [project-indexer.md](project-indexer.md) |
| Context Store | [context-store.md](context-store.md) |
| Vector Store | [vector-store.md](vector-store.md) |
| Prompt Builder | [prompt-builder.md](prompt-builder.md) |
| Event Bus | [event-bus.md](event-bus.md) |
| Mode Runtime | [mode-runtime.md](mode-runtime.md) |
| API endpoints | [api.md](api.md) |
| Agent integration | [integration.md](integration.md) |

### Progress

See [PROGRESS.md](PROGRESS.md) for implementation status and session handoff notes.

### Related docs

- [Agent prompt composition](../agents/prompt-composition.md)
- [Agent adapters](../agents/README.md)
