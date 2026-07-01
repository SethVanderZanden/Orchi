# Prompt Builder

## Dummy section (start here)

Every agent request is a **form letter**: company letterhead up top (stable), today's message at the bottom (dynamic). SharedContext assembles both parts so providers can cache the letterhead.

| Analogy | Orchi |
|---------|-------|
| Letterhead | `BuildStablePrefix` — mode instructions + project rules |
| Today's letter | `BuildDynamicContext` — summary, retrieval, history, user msg |
| Mail room | `AgentPromptComposer` in API wires `ChatSession` → `PromptSessionContext` |

---

## Status

**done** — see [PROGRESS.md](PROGRESS.md)

## Stable prefix

1. Mode instructions (`ModeInstructions.*` from API)
2. `AGENTS.md`, `CLAUDE.md`
3. `.cursor/rules/*.mdc`
4. Technology stack detection

## Dynamic context

1. Session summary (from context store)
2. Mode transition marker (when mode changed)
3. Git branch/head
4. Retrieved chunks (vector store)
5. Conversation history (safety net: 10 with resume, 50 without)
6. Middle section (attached plan, etc.)
7. Current user content

## Key types

- `IPromptBuilder` — [`src/SharedContext/Prompts/PromptBuilder.cs`](../../src/SharedContext/Prompts/PromptBuilder.cs)
- `AgentTurnRequest` — now has `StablePrefix`, `DynamicContext`, `CliProfileKind`
- Canonical rules: [prompt-composition.md](../agents/prompt-composition.md)

## Tests

Strategy tests use `AgentPromptComposerTestFactory` in `tests/Orchi.Api.Tests/`.
