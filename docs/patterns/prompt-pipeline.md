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

Prompt composition lives under `src/API/Infrastructure/Agents/Modes/Prompt/`. At send time, `AgentPromptComposer` builds a `PromptBuildContext` from the `ChatSession` and user message, runs a pipeline of section contributors, and renders the result as XML.

```
AgentSessionManager.SendMessageAsync
    → AgentPromptComposer.Compose(session, userContent)
        → PromptSectionPipeline (contributors)
        → OrchiPromptRenderer
    → CursorAgentAdapter (single prompt string)
```

## Section contributors

Contributors run in registration order (see `AgentsExtensions.cs`):

| Contributor | Section(s) | When |
|-------------|------------|------|
| `ModeSectionContributor` | identity, rules, context, tools | Delegates to `IAgentModeStrategy` |
| `SessionContextContributor` | context | Appends workspace path |
| `SessionTaskContributor` | task | When `ChatSession.PlanFilePath` is set |
| `GlobalRulesContributor` | rules | Every turn — meta-rule about focusing on `<message>` |
| `MessageContributor` | message | Every turn — raw user content |

## Mode strategies

Each mode implements `IAgentModeStrategy.ContributeSections`:

| Mode | Sections populated |
|------|-------------------|
| `default` | None (global rules + message only) |
| `orchestration` | identity, rules, context (plan block template) |

Add a new mode by implementing `IAgentModeStrategy`, registering it in `AgentsExtensions.cs`, and adding the mode id to the desktop UI.

## XML rendering

`OrchiPromptRenderer` wraps non-empty sections in `<orchi>...</orchi>`. Empty sections are omitted. Content with `<`, `>`, or `&` is wrapped in CDATA; otherwise plain text is emitted.

## Global meta-rule

Every prompt includes a rule telling the agent not to acknowledge or respond to instruction sections. The user's conversational input is always in `<message>`. When `<task>` is present (e.g. a kicked-off plan), it describes assigned work — not casual chat like "hi".

## Related docs

- [Agent adapters](../agents/README.md) — message flow and orchestration kick-off
- [Decorator pattern](decorator.md) — same pipeline spirit as CQRS behaviours
- [CQRS Pipeline](../architecture/cqrs-pipeline.md) — handler decorator chain
