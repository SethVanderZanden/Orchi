---
name: orchi-agent-prompts
description: >-
  Enforce stable-prefix / dynamic-context prompt composition for Orchi agent
  requests. Use when editing src/API/Infrastructure/Agents/Modes/, AgentTurnRequest,
  ModeInstructions, mode strategies, CursorAgentAdapter prompt assembly, or when
  the user mentions prompt caching, stable prefix, or prompt composition.
---

# Orchi Agent Prompt Composition

Canonical reference: [docs/agents/prompt-composition.md](../../../docs/agents/prompt-composition.md)

## Provider caching rule

To benefit from provider prompt caching, structure every agent request like this:

**[STABLE PREFIX — rarely changes]**

- System instructions
- Tool definitions
- Coding standards
- Repo architecture summary
- Project rules
- AGENTS.md / CLAUDE.md content

**[DYNAMIC CONTEXT — changes often]**

- Current task
- Relevant files
- Recent diffs
- Errors
- User message

## Enforcement checklist

When changing prompt assembly code, verify:

- [ ] Variable content goes **after** `---`, never before mode instructions
- [ ] `ModeInstructions` constants stay stable — no timestamps, session IDs, or per-turn data
- [ ] Attached plans, check-in payloads, diffs, and errors are in the **dynamic suffix**
- [ ] New context sources (`.cursor/rules`, `AGENTS.md`) belong in a **shared stable-prefix builder**, not scattered in mode strategies
- [ ] Per-mode prefix stays consistent across turn types (Goal/GoalCheckIn switching is a known exception — document if adding similar)
- [ ] If refactoring `AgentTurnRequest`, use explicit `StablePrefix` + `DynamicContext` fields before concatenating for the CLI

## Key files

| File | Role |
|------|------|
| `src/API/Infrastructure/Agents/Modes/AgentTurnRequest.cs` | Turn payload passed to adapter |
| `src/API/Infrastructure/Agents/Modes/Strategies/ModeInstructions.cs` | Stable mode instruction constants |
| `src/API/Infrastructure/Agents/Modes/Strategies/*ModeStrategy.cs` | Per-mode `PrepareTurn` composition |
| `src/API/Infrastructure/Agents/Cursor/CursorAgentAdapter.cs` | CLI spawn; prompt is final positional arg |
| `src/API/Infrastructure/Agents/AgentSessionManager.cs` | Turn orchestration |

## Boundaries

Orchi does **not** assemble tool definitions or control the Cursor CLI internal system prompt. Conversation continuity uses `--resume` (`ChatSession.ExternalSessionId`), not Orchi-owned history replay in the prompt string.

## When implementing caching

Read the "Future implementation" section in [prompt-composition.md](../../../docs/agents/prompt-composition.md). Introduce a centralized `PromptComposer` or split `AgentTurnRequest` before adding provider cache markers.
