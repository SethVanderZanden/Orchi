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
| `cursor` | `CursorAgentAdapter` | Implemented |
| `codex` | `CodexAgentAdapter` | Implemented — see [codex.md](codex.md) |
| `claude` | — | Interface only; see [adapters.md](adapters.md) |

## Key types

- **`IAgentAdapter`** — send a message for a session; yield `AgentEvent` stream
- **`AgentSessionManager`** — write-through cache over `IChatStore`, process lifecycle, turn execution
- **`IChatStore`** — EF-backed persistence (SQLite); in-memory implementation for unit tests
- **`ChatSession`** — Orchi chat id, agent id, workspace path, external resume id, optional model slug, optional context size / reasoning effort / approval policy, hydrated `CliConfigOverrides`, running process handle

## Mode runtime defaults

Each Orchi mode (`default`, `orchestration`, …) has a settings-backed tuple: **agent + model + context size + CLI options** (reasoning effort, approval policy). Creating a chat or changing mode applies that tuple. See Settings → Agents → Mode defaults. Codex `-c` knobs are user-populated catalogs — see [codex.md](codex.md).

## Event scripting and git workflows

Named scripts (global or project) bind to **agent start/finish** with optional mode filters. Steps include shell commands and git actions (`commit`, `push`, `merge`, `createPullRequest`, `worktree`). Hosted PRs use pluggable adapters for **GitHub (`gh`)** and **Azure DevOps (`az`)** — see [event-scripting.md](../patterns/event-scripting.md).

Plan kickoff can provision a `WorkspaceKind.Worktree` from the project `DefaultBaseBranch`. Apply the orchestration git template from Settings → Workflows.

## Agent model catalog

Each agent can expose a model list (Cursor via `agent --list-models`). Orchi stores catalog entries in SQLite with enable/disable curation and caches list reads in HybridCache (24h default).

| Endpoint | Purpose |
|----------|---------|
| `GET /agents/{agentId}/models` | List enabled (or all) models for dropdown/settings |
| `POST /agents/{agentId}/models/sync` | Refresh from CLI; absent models are disabled, not deleted |
| `POST /agents/{agentId}/models` | Manually add a model slug |
| `PATCH /agents/{agentId}/models` | Enable/disable a catalog entry (`{ modelId, enabled }` body) |
| `POST /agents/{agentId}/models/remove` | Remove a catalog entry by `modelId` in the body |
| `PATCH /chats/{chatId}/model` | Set per-chat model (`null` = CLI default) |

Child chats created via plan/review kickoff inherit the parent's `ModelId`.

## Message flow

Each turn is intentionally simple:

1. Persist the user message
2. Pass the raw user text to `IAgentAdapter.SendMessageAsync`
3. Stream `AgentEvent` results back to the client
4. Persist the assistant message and `ExternalSessionId` when the turn completes

Multi-turn continuity uses Cursor `--resume` with the stored `ExternalSessionId`. **Agent modes** wrap prompts in an `<orchi>` XML envelope at the CLI boundary (see below).

Sessions and messages persist to **SQLite** via EF Core (`orchi.db`). User messages are saved immediately; assistant messages are saved once when a turn completes or errors (streaming tokens stay in-memory only). Active chats are cached in memory for streaming and process handles; `GET /chats` and `GET /chats/{id}` hydrate from the database when needed.

| Analogy | Code |
|---------|------|
| Filing cabinet | `IChatStore` / `EfChatStore` |

## Agent modes vs agent adapters

| Concept | Answers | Example |
|---------|---------|---------|
| **Agent adapter** | Which CLI provider? | `cursor` |
| **Agent mode** | How is the prompt shaped? | `default`, `orchestration`, `review`, `implementation` (kickoff-only) |

Agent modes use a strategy + [prompt pipeline](../patterns/prompt-pipeline.md#dummy-section-start-here) under `Infrastructure/Agents/Modes/`:

- `IAgentModeStrategy` — contributes mode-specific `<orchi>` sections and optional CLI args
- `IAgentModeStrategyFactory` — resolve strategy by mode id
- `AgentPromptComposer` — runs the section pipeline and renders the final XML prompt

User messages are stored **raw** in the database; composition happens only at the CLI boundary.

### `<orchi>` prompt sections

| Section | Meaning | Who sets it |
|---------|---------|-------------|
| `<identity>` | Who the agent is in this mode | Mode strategy |
| `<rules>` | Behavioral constraints + global meta-rules | Mode strategy + pipeline |
| `<context>` | Background (workspace, templates) | Mode strategy + session |
| `<tools>` | Tool/MCP hints (future) | Mode strategy |
| `<task>` | Assigned work item (e.g. plan to implement) | Session when `PlanFilePath` is set |
| `<message>` | The user's actual chat input | Always from user content |

A global meta-rule tells the agent not to respond to instruction sections — only to `<message>` (and to treat `<task>` as assigned work when present). See [Prompt pipeline](../patterns/prompt-pipeline.md).

### Orchestration mode

`orchestration` is an enhanced plan mode. The orchestrator decomposes work into several small plans wrapped in `<!-- orchi-plan:id -->` markers. Each plan can be **kicked off** via `POST /chats/{parentChatId}/plans/kickoff`, which:

1. Writes `.orchi/plan-{id}.md` in the workspace
2. Creates a child chat in `implementation` mode (kickoff-only; not listed in `GET /agents/modes`)
3. Returns `initialPrompt` (for sidebar preview) and `kickoffMessage` (`"Begin implementation."`); the desktop auto-sends `kickoffMessage` to the child agent
4. The child agent deletes the plan file after successful implementation and validation (if blocked, the plan file is kept)

```
Orchestration chat  →  plans in assistant output  →  kick off  →  .orchi/plan-*.md + child chat
```

#### Sequential plan kickoff

#### Dummy section (start here)

Imagine a relay race: runner 1 must finish their lap before runner 2 starts, but a separate sprinter on another track can go whenever they want.

In Orchi orchestration, some plans are **independent** (parallel sprinters) and some are **sequenced** (relay runners). The orchestrator can emit a machine-readable order list so **Kick off all** knows who runs when.

| Analogy | Orchi |
|---------|-------|
| Relay order list | `<!-- orchi-plan-sequence -->` block |
| One runner at a time | Sequenced plans kick off one implementation at a time |
| Sprinter on another track | Plans not in the sequence block still start in parallel |
| Next runner starts when previous finishes | Next sequenced plan starts when the prior **implementation** child completes (review does not gate the queue) |

**Aha:** The API runs the relay — the desktop watches the scoreboard via SSE.

---

When the orchestrator must run plans in order, it emits a sequence block after the plan blocks in the same assistant message:

```markdown
<!-- orchi-plan-sequence -->
first-plan-id
second-plan-id
third-plan-id
<!-- /orchi-plan-sequence -->
```

Rules:
- One kebab-case plan ID per line; each ID must match an `orchi-plan:id` marker in the same output.
- Order in the block is execution order.
- Plans **not** listed remain independent and kick off in parallel with each other (and with the first sequenced plan).
- When **all** plans must run in order, list every plan ID in the block.
- Keep the human **Dependencies and sequencing** section in each plan; the sequence block is the machine-readable source of truth for kickoff.

**Kick off all** behavior:
- **Endpoint:** `POST /chats/{parentChatId}/orchestration/kickoff-all` starts/resumes the workflow (replaces client-side queue logic).
- **State:** `GET /chats/{parentChatId}/orchestration` returns parsed plans, sequence, workflow status, and child mappings. Workflow state persists in SQLite.
- **Events:** `GET /chats/{parentChatId}/orchestration/events` SSE broadcasts workflow progress, parent status messages, and multiplexed child agent events.
- **No sequence block:** all pending plans kick off in parallel.
- **Sequence block present:** button reads **Kick off in order (N)**; only the first pending sequenced plan starts. When that implementation child completes, the API auto-starts the next sequenced plan. Review for a completed plan may run concurrently with the next implementation.
- If an implementation child ends with an error, the workflow pauses (`paused` status); remaining plans can still be kicked off manually.

Post-implementation steps (review kickoff, sequential advance) run via an extensible `IOrchestrationStepHandler` pipeline in `Infrastructure/Agents/Orchestration/`.

#### Implementation mode and Cursor cache reads

Plan kickoff child chats use **`implementation`** mode with scoped rules: read the plan file first, stay within the plan's Affected files list, and avoid broad repo exploration. The `<task>` section (plan path + delete-after-validation instructions) is included on the **first user turn only**; follow-up messages rely on Cursor `--resume` session continuity.

**Cache Read** in Cursor usage dashboards is **Cursor/Anthropic prompt-cache billing**, not Orchi HybridCache (see [Caching](#caching) below). During one kicked-off plan, Orchi typically sends one user message; Cursor then runs many internal LLM rounds (file reads, edits, validation). Each round re-reads the accumulated cached prefix, so Cache Read can dominate the usage breakdown even when Input looks small.

Scoped implementation rules and deduplicated kickoff prompts reduce context growth, but multi-file plans still incur Cache Read proportional to agent rounds × context size. Orchestration plans should list every file the implementation agent needs to read under Affected files.

### Review mode

After an implementation child agent completes, the API automatically kicks off a **review child** in `review` mode via the orchestration step pipeline (same outcome as `POST /chats/{implementationChildChatId}/review/kickoff`). The review agent reads the git diff + original plan and outputs short `<!-- orchi-review-plan:id -->` blocks meant for humans to skim — TLDR first, then oversights / over-engineering / missed patterns. It does not restate the changelog.

At prompt composition time, Orchi runs **`git diff HEAD`** in the workspace (falling back to **`git show HEAD`** when there are no uncommitted changes) and appends the result to the review agent's `<context>` section. The review agent does not need to run git itself. The `IWorkspaceDiffProvider` abstraction allows swapping in snapshot-based diffs later.

```
Implementation child completes  →  auto review kickoff  →  .orchi/review-*.md + review child
  →  git diff appended to <context>  →  orchi-review-plan blocks  →  parent highlights review
```

The orchestration parent highlights review-ready plan cards and opens the review panel when review plans appear.

#### Branch / PR review (without orchestration)

You can also shoot off a review for any branch against another branch (pull-request style), without an implementation child:

1. Desktop: **Git → Review branch…** (or finder command **Review branch**)
2. API: `POST /projects/{projectId}/reviews/from-branches` with `{ headBranch, baseBranch?, fetch? }`
3. Orchi optionally runs `git fetch --prune --all`, lists branches via `GET /projects/{id}/branches?fetch=true`, creates a worktree on the head branch, writes `.orchi/review-branch-*.md`, opens a `review` chat, and the desktop auto-sends `Begin review.`

The review brief embeds `<!-- orchi-branch-review head: … base: … -->`. `ReviewDiffContributor` detects that marker and injects **`git diff base...head`** (three-dot / merge-base) instead of `git diff HEAD`.

```
Pick head + base  →  fetch/list branches  →  worktree on head  →  review chat + auto-send
  →  git diff base...head in <context>  →  orchi-review-plan blocks
```

### Artifact files (Strategy + Factory)

Plan and review briefs share the same write operation but use different path templates under `.orchi/`:

| Kind | Path | Purpose |
|------|------|---------|
| `plan` | `.orchi/plan-{id}.md` | Implementation work for a child agent |
| `review` | `.orchi/review-{id}.md` | Review brief for a review child agent |

`IOrchiArtifactWriterStrategy` + `OrchiArtifactWriterFactory` select the write policy at runtime (mirrors agent mode registration). `IOrchiArtifactTaskFactory` maps the session's `PlanFilePath` to the correct `<task>` prompt.

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

## Caching

Orchi uses **Microsoft HybridCache** (memory-only L1 today) behind `IOrchiCacheService` so repeated read-heavy work can skip redundant I/O. Think of it as a **memo pad on the front desk**: the first guest asking “what changed in git?” triggers a walk to the file room; the next guest with the same question gets the note already on the pad — until the repo moves forward (new commit) or the note expires.

| Analogy | Code |
|---------|------|
| Memo pad | `IOrchiCacheService` / `OrchiHybridCacheService` |
| Note categories | `OrchiCacheKeys` (workspace diff, Cursor executable, plan, agent models) |
| Fresh copy after edits | `CachingPlanStore` invalidates on `UpsertAsync` |

**Cached today:**

| Path | TTL (default) | Key includes |
|------|---------------|--------------|
| Git workspace diff (`IWorkspaceDiffProvider`) | 30s | Normalized workspace path + `git rev-parse HEAD` |
| Cursor executable resolution | 60m | Executable config fingerprint |
| Plan store reads (`IPlanStore.GetAsync`) | 10m | Source chat id + plan id; invalidated on upsert |
| Agent model catalog (`IAgentModelCatalogService.ListAsync`) | 24h | Agent id + include-disabled flag; invalidated on sync/curation |

**Not cached (intentionally):**

- `AgentSessionManager._sessions` — live runtime state (process handles, locks); write-through over `IChatStore`
- `IChatStore` list/get — deferred; merges with in-memory sessions and mutates frequently

Configure under `Cache` in `appsettings.json`. `Cache:Distributed:Enabled` is `false` by default; Redis L2 is a future config-only hook — no Redis package yet.

## Lifecycle

1. **Create chat** — `POST /chats` with `{ agent, workspacePath, mode? }`; validates path; persists chat row; no CLI yet
2. **Send message** — `POST /chats/{id}/messages` → SSE stream; spawns/resumes Cursor CLI per turn; persists messages on completion
3. **Kick off plan** — `POST /chats/{parentChatId}/plans/kickoff` (orchestration chats only); writes plan file, creates child chat, and instructs the agent to delete the plan file after validation
4. **Kick off all / workflow** — `POST /chats/{parentChatId}/orchestration/kickoff-all`; backend runs sequenced and parallel kickoffs, emits orchestration SSE events
5. **Kick off review** — `POST /chats/{implementationChildChatId}/review/kickoff` (also auto-triggered by orchestration workflow after implementation completes); writes review brief, creates review child chat
5b. **Kick off branch review** — `POST /projects/{projectId}/reviews/from-branches`; fetch/list branches, worktree on head, write branch review brief, create review chat (desktop auto-sends kickoff)
6. **Close chat** — `DELETE /chats/{id}` → kills process, soft-deletes chat in database
7. **App shutdown** — `POST /chats/shutdown` (called from Electron `before-quit`) → kills active sessions (persisted chats remain in database)

## Further reading

- [Cursor CLI integration](cursor-cli.md) — install, flags, NDJSON parsing
- [Concurrent stdout/stderr reading](concurrent-pipe-reading.md#dummy-section-start-here) — why stderr is started before the stdout loop
- [Adding adapters](adapters.md) — `IAgentAdapter` contract and extension steps
- [Contributor pattern](../patterns/contributor.md) — `IPromptSectionContributor` and the shared-document pipeline
- [Prompt pipeline](../patterns/prompt-pipeline.md) — `<orchi>` XML envelope and end-to-end compose flow
- [Frontend chat streaming](../frontend/chat-streaming.md) — SSE contract and Marker UI
