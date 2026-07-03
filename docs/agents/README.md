# Agent adapters

## Dummy section (start here)

Think of Orchi as a **front desk** at a hotel. Guests (the desktop chat UI) never walk into the kitchen themselves — they ask the front desk for food. The front desk knows which **chef** to call: Cursor today, maybe Codex or Claude later.

Each chef has their own way of working (CLI flags, output format, session IDs). The **adapter** is the translator: one stable Orchi language in, one chef-specific process out, events back in a shape the UI understands.

```
Desktop chat  →  Orchi API  →  IAgentAdapter  →  Cursor CLI (or future agents)
                     ↑                              ↓
                     └──────── SSE stream ──────────┘
```

**Orchi translation:**

| Analogy | Code |
|---------|------|
| Front desk | `AgentSessionManager` + chat endpoints |
| Chef roster | `IAgentAdapterFactory` |
| Cursor chef | `CursorAgentAdapter` |
| Order ticket | `ChatSession` (workspace, agent id, resume id) |
| Kitchen chatter | NDJSON stdout → `AgentEvent` → SSE to desktop |

Everything below is the same idea with file paths and extension points.

---

## Overview

Orchi runs AI agents from the **.NET API**, not from Electron. The desktop sends HTTP requests; the API spawns local CLI processes, parses their output, and streams results to the UI via **Server-Sent Events (SSE)**.

Agent integration lives under `src/API/Infrastructure/Agents/`.

## Current agents

| Agent ID | Adapter | Status |
|----------|---------|--------|
| `cursor` | `CursorAgentAdapter` | Implemented (v1) |
| `codex` | — | Interface only; see [adapters.md](adapters.md) |
| `claude` | — | Interface only; see [adapters.md](adapters.md) |

## Key types

- **`IAgentAdapter`** — send a message for a session; yield `AgentEvent` stream
- **`AgentSessionManager`** — write-through cache over `IChatStore`, process lifecycle, turn execution
- **`IChatStore`** — EF-backed persistence (SQLite); in-memory implementation for unit tests
- **`ChatSession`** — Orchi chat id, agent id, workspace path, Cursor resume id, running process handle

## Message flow

Each turn is intentionally simple:

1. Persist the user message
2. Pass the raw user text to `IAgentAdapter.SendMessageAsync`
3. Stream `AgentEvent` results back to the client
4. Persist the assistant message and `ExternalSessionId` when the turn completes

Multi-turn continuity uses Cursor `--resume` with the stored `ExternalSessionId`. By default, Orchi passes user text verbatim to the CLI. **Agent modes** can prepend static instructions before the user message (see below).

Sessions and messages persist to **SQLite** via EF Core (`orchi.db`). User messages are saved immediately; assistant messages are saved once when a turn completes or errors (streaming tokens stay in-memory only). Active chats are cached in memory for streaming and process handles; `GET /chats` and `GET /chats/{id}` hydrate from the database when needed.

| Analogy | Code |
|---------|------|
| Filing cabinet | `IChatStore` / `EfChatStore` |

## Agent modes vs agent adapters

| Concept | Answers | Example |
|---------|---------|---------|
| **Agent adapter** | Which CLI provider? | `cursor` |
| **Agent mode** | How is the prompt shaped? | `default`, `orchestration` |

Agent modes use a strategy + factory pattern under `Infrastructure/Agents/Modes/`:

- `IAgentModeStrategy` — static instructions and optional CLI args per mode
- `IAgentModeStrategyFactory` — resolve strategy by mode id
- `AgentPromptComposer` — builds `{instructions}\n\n---\n\nUser message:\n{content}`

User messages are stored **raw** in the database; composition happens only at the CLI boundary so the static instruction prefix can be cached by the agent.

### Orchestration mode

`orchestration` is an enhanced plan mode. The orchestrator decomposes work into several small plans wrapped in `<!-- orchi-plan:id -->` markers. Each plan can be **kicked off** via `POST /chats/{parentChatId}/plans/kickoff`, which:

1. Writes `.orchi/plan-{id}.md` in the workspace
2. Creates a child chat in `default` mode
3. Returns an implementation prompt; the desktop auto-sends it to the child agent

```
Orchestration chat  →  plans in assistant output  →  kick off  →  .orchi/plan-*.md + child chat
```

## Configuration

`appsettings.json`:

```json
"Agents": {
  "Cursor": {
    "Executable": "agent",
    "DefaultArgs": ["--force", "--trust"],
    "TimeoutSeconds": 600
  }
}
```

The Cursor CLI must be installed and authenticated on the **same machine as the API**.

## Lifecycle

1. **Create chat** — `POST /chats` with `{ agent, workspacePath, mode? }`; validates path; persists chat row; no CLI yet
2. **Send message** — `POST /chats/{id}/messages` → SSE stream; spawns/resumes Cursor CLI per turn; persists messages on completion
3. **Kick off plan** — `POST /chats/{parentChatId}/plans/kickoff` (orchestration chats only); writes plan file and creates child chat
4. **Close chat** — `DELETE /chats/{id}` → kills process, soft-deletes chat in database
5. **App shutdown** — `POST /chats/shutdown` (called from Electron `before-quit`) → kills active sessions (persisted chats remain in database)

## Further reading

- [Cursor CLI integration](cursor-cli.md) — install, flags, NDJSON parsing
- [Concurrent stdout/stderr reading](concurrent-pipe-reading.md#dummy-section-start-here) — why stderr is started before the stdout loop
- [Adding adapters](adapters.md) — `IAgentAdapter` contract and extension steps
- [Frontend chat streaming](../frontend/chat-streaming.md) — SSE contract and Marker UI
