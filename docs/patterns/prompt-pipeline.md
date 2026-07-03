# Prompt Pipeline

## Dummy section (start here)

Imagine packing a **labeled lunchbox** for an agent. You don't dump everything in one pile — you put the sandwich in the sandwich slot, the note from home in the note slot, and the assignment sheet in its own pocket. The agent opens the box, reads the labels, and knows what to focus on.

Orchi does the same with prompts. Instead of one long blob of text, each chat turn gets an `<orchi>` envelope with labeled sections: who the agent is, what rules apply, background context, assigned work, and the user's actual message.

```
User types "hi"  →  pipeline fills sections  →  <orchi><rules>...</rules><message>hi</message></orchi>  →  Cursor CLI
```

**Key idea:** Instruction sections are background. The agent should respond to `<message>`, not recite the rules back to you.

**Orchi translation:**

| Lunchbox slot | Orchi section |
|---------------|---------------|
| Name tag on the box | `<identity>` |
| Rules from teacher | `<rules>` |
| Worksheets / templates | `<context>` |
| Homework assignment | `<task>` |
| Student's note | `<message>` |

Everything below is the same idea with C#, DI, and the contributor pipeline.

---

## Overview

Prompt composition lives under `src/API/Infrastructure/Agents/Modes/Prompt/`. At send time, `IAgentPromptComposer` builds a `PromptBuildContext` from the `ChatSession` and user message, runs a pipeline of section contributors, and renders the result as XML.

For the general **contributor pattern** (shared document, many independent writers, DI registration order), see [Contributor pattern](contributor.md).

```
AgentSessionManager.SendMessageAsync
    → IAgentPromptComposer.Compose(session, userContent)
        → compose decorators (logging, debug artifacts)
        → AgentPromptComposer (core)
            → PromptSectionPipeline (contributors)
            → OrchiPromptRenderer
    → CursorAgentAdapter (single prompt string)
```

## Three extension axes

| Axis | Interface | Register in | Example |
|------|-----------|-------------|---------|
| Session/global sections | `IPromptSectionContributor` | `AgentsExtensions.cs` — **order matters** | `ParentChatContributor`, `GlobalRulesContributor` |
| Mode-specific sections | `IAgentModeStrategy` | Same file; desktop loads labels from `GET /agents/modes` | `OrchestrationAgentModeStrategy` |
| XML output shape | `OrchiPromptRenderer.SectionOrder` | Hard-coded in renderer | Add `<tools>` support |

Use **contributors** when multiple sources can append to the same section (`AppendRules`, `AppendContext`). Use **mode strategies** when a chat mode defines a coherent personality (identity + rules + templates). Use the **renderer** only when adding or reordering XML tags.

## Section contributors

Contributors run in registration order (see `AgentsExtensions.cs`):

| Contributor | Section(s) | When |
|-------------|------------|------|
| `ModeSectionContributor` | identity, rules, context, tools | Delegates to `IAgentModeStrategy` |
| `SessionContextContributor` | context | Appends workspace path |
| `SessionTaskContributor` | task | When `ChatSession.PlanFilePath` is set — instructs implement + delete plan file after validation |
| `ParentChatContributor` | context | When `ChatSession.ParentChatId` is set |
| `GlobalRulesContributor` | rules | Every turn — meta-rule about focusing on `<message>` |
| `MessageContributor` | message | Every turn — raw user content |

### Adding a contributor

1. Implement `IPromptSectionContributor` in `Modes/Prompt/`
2. Register in `AgentsExtensions.cs` **before** `MessageContributor`
3. Use `document.AppendRules(...)` / `document.AppendContext(...)` when multiple writers share a section; assign directly when one writer owns it (`document.Task = ...`)
4. Add pipeline and/or composer tests

## Mode strategies

Each mode implements `IAgentModeStrategy` with `ModeId`, `DisplayLabel`, `Description`, and `ContributeSections`:

| Mode | Sections populated |
|------|-------------------|
| `default` | None (global rules + message only) |
| `orchestration` | identity, rules, context (plan block template) |

### Adding a new mode

1. Add a mode id constant to `AgentModeIds` (or reuse an existing one)
2. Create `IAgentModeStrategy` — see `OrchestrationAgentModeStrategy.cs`
3. `services.AddSingleton<IAgentModeStrategy, YourModeStrategy>()` in `AgentsExtensions.cs`
4. The desktop **New chat** dialog loads modes from `GET /agents/modes` — no hardcoded list update required if `DisplayLabel` and `Description` are set on the strategy

Mode-specific UI (e.g. plan cards for orchestration) still compares `chat.mode` to known ids in the desktop.

## Compose decorators

Cross-cutting concerns wrap `IAgentPromptComposer` via Scrutor `Decorate` (same pattern as CQRS behaviours in `PipelineExtensions.cs`):

| Decorator | Purpose |
|-----------|---------|
| `LoggingPromptComposer` | Logs compose start and prompt length |

Register new compose decorators in `AgentsExtensions.cs`. Last registered = outermost. Do **not** use decorators for section content — use contributors instead.

## XML rendering

`OrchiPromptRenderer` wraps non-empty sections in `<orchi>...</orchi>`. Empty sections are omitted. Content with `<`, `>`, or `&` is wrapped in CDATA; otherwise plain text is emitted.

### Adding a new XML section

1. Add property to `OrchiPromptDocument` (+ `AppendX` if multiple writers)
2. Add entry to `OrchiPromptRenderer.SectionOrder`
3. Populate via contributor or mode strategy
4. Add renderer and composer tests

## Global meta-rule

Every prompt includes a rule telling the agent not to acknowledge or respond to instruction sections. The user's conversational input is always in `<message>`. When `<task>` is present (e.g. a kicked-off plan), it describes assigned work — not casual chat like "hi".

## Related docs

- [Contributor pattern](contributor.md) — the general pattern behind `IPromptSectionContributor`
- [Agent adapters](../agents/README.md) — message flow and orchestration kick-off
- [Decorator pattern](decorator.md) — CQRS behaviour stack; compose decorators use the same Scrutor pattern
- [CQRS Pipeline](../architecture/cqrs-pipeline.md) — handler decorator chain
