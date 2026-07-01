# Mode Runtime

## Dummy section (start here)

Orchi modes are like **hats** the agent wears (planner, builder, participant). The mode runtime picks the right hat and decides whether to keep the same walkie-talkie channel (Cursor `--resume`) when switching hats.

| Analogy | Orchi |
|---------|-------|
| Hat type | `CursorCliProfileKind` (Agent, Plan, Ask) |
| Walkie-talkie channel | `ExternalSessionId` / `--resume` |
| "You switched hats" note | Mode transition marker in dynamic prompt |

---

## Status

**done** — see [PROGRESS.md](PROGRESS.md)

## CLI profile mapping

| Orchi mode | Profile | CLI args |
|------------|---------|----------|
| `agent`, `implement` | Agent | none |
| `plan`, `orchestrate`, `goal` | Plan | `--mode=plan` |
| `participant`, goal check-in | Ask | `--mode=ask` |

## Resume preservation

`ShouldPreserveResume` returns true only when `CursorCliProfileKind` unchanged across mode switch. Example: `plan → orchestrate` preserves; `participant → plan` clears.

`AgentSessionManager.UpdateModeAsync` uses this policy instead of always clearing `ExternalSessionId`.

## Boundaries

Orchi controls mode via **prompts + CLI profile**. Individual Cursor tools are not gated per-tool today — see [agents README](../agents/README.md).

Implementation: [`src/SharedContext/Modes/ModeRuntime.cs`](../../src/SharedContext/Modes/ModeRuntime.cs)

Tests: `tests/Orchi.Api.Tests/Infrastructure/SharedContext/ModeRuntimeTests.cs`
