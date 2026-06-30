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
| Order ticket | `ChatSession` (workspace, agent id, mode, resume id) |
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
- **`AgentSessionManager`** — write-through cache over `IChatStore`, process lifecycle, turn orchestration
- **`IChatStore`** / **`IPlanStore`** — EF-backed persistence (SQLite); in-memory implementations for unit tests
- **`ChatSession`** — Orchi chat id, agent id, **chat mode**, workspace path, Cursor resume id, parent/goal/plan links, running process handle

## Chat modes vs agents

**Agent** (`IAgentAdapter`) = which runtime spawns the CLI (e.g. `cursor`).

**Mode** (`IChatModeStrategy`) = behavior for a chat: prompt instructions, CLI flags, validation, orchestration hooks.

| Mode | Cursor CLI | Orchi behavior |
|------|------------|----------------|
| `agent` | default agent | General coding assistant (default mode) |
| `plan` | `--mode=plan` | Standard planning |
| `implement` | default agent | Requires `attachedPlanId`; injects plan content |
| `orchestrate` | `--mode=plan` | Parses sub-plans; dispatch + handoff APIs |
| `goal` | `--mode=ask` / `plan` | Event-driven check-ins on child chat activity |

Modes are set at `POST /chats` (default `agent`) and can be changed mid-lifecycle via `PATCH /chats/{id}` — the new mode applies to the **next message** only. Cannot change mode while a turn is in progress. See `src/API/Infrastructure/Agents/Modes/`.

### Prompt composition

Each mode strategy builds a `PreparedPrompt` by prepending stable mode instructions, then a `---` delimiter, then per-turn user content. See [prompt composition](prompt-composition.md#dummy-section-start-here) for the stable-prefix / dynamic-context pattern and provider caching guidance.

Sessions and messages persist to **SQLite** via EF Core (`orchi.db`). User messages are saved immediately; assistant messages are saved once when a turn completes or errors (streaming tokens stay in-memory only). Plans and sub-plans are also persisted. Active chats are cached in memory for streaming and process handles; `GET /chats` and `GET /chats/{id}` hydrate from the database when needed.

| Analogy | Code |
|---------|------|
| Filing cabinet | `IChatStore` / `EfChatStore` |
| Recipe cards | `IPlanStore` / `EfPlanStore` |

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

1. **Create chat** — `POST /chats` with `{ agent, workspacePath, mode?, parentChatId?, attachedPlanId? }`; validates path; persists chat row; no CLI yet
2. **Update mode** — `PATCH /chats/{id}` with `{ mode, attachedPlanId? }`; rejects if a message is in progress
3. **Send message** — `POST /chats/{id}/messages` → SSE stream; spawns/resumes Cursor CLI per turn; persists messages on completion
4. **Close chat** — `DELETE /chats/{id}` → kills process, soft-deletes chat in database
5. **App shutdown** — `POST /chats/shutdown` (called from Electron `before-quit`) → kills active sessions (persisted chats remain in database)

## Further reading

- [Prompt composition](prompt-composition.md#dummy-section-start-here) — stable prefix vs dynamic context, provider caching
- [Cursor CLI integration](cursor-cli.md) — install, flags, NDJSON parsing
- [Concurrent stdout/stderr reading](concurrent-pipe-reading.md#dummy-section-start-here) — why stderr is started before the stdout loop
- [Adding adapters](adapters.md) — `IAgentAdapter` contract and extension steps
- [Frontend chat streaming](../frontend/chat-streaming.md) — SSE contract and Marker UI
