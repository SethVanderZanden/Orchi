# Contributor Pattern

## Dummy section (start here)

Imagine a **group report** where everyone writes one section of the same document. Nobody owns the whole thing — each person reads the assignment sheet, adds their part to the shared Google Doc, and passes it on. The teacher collects one finished report at the end.

```
Assignment sheet  →  Person A adds intro  →  Person B adds data  →  Person C adds conclusion  →  final report
```

That is the contributor pattern: **many small writers, one shared output, run in a fixed order.**

It is different from a **decorator** (see [Decorator](decorator.md)): a decorator wraps one object and intercepts a single call (`Handle`). Contributors do not wrap each other — they all write into the same document independently.

**Orchi translation:**

| Group report | Orchi |
|--------------|-------|
| Assignment sheet | `PromptBuildContext` (mode, workspace, user text, plan path, …) |
| Shared Google Doc | `OrchiPromptDocument` (identity, rules, context, task, message, …) |
| Each person | `IPromptSectionContributor` |
| "Everyone go in this order" | `PromptSectionPipeline` — runs contributors in DI registration order |
| Printed final report | `OrchiPromptRenderer` → `<orchi>` XML string |

**Key idea:** Add a new prompt concern by adding one class — no changes to existing contributors or the pipeline loop.

Everything below is the same idea with C#, DI, and the agent prompt pipeline.

---

Orchi uses the **contributor pattern** to build agent prompts: many independent classes each add content to a shared `OrchiPromptDocument`, orchestrated by `PromptSectionPipeline`.

Implementation: [`Infrastructure/Agents/Modes/Prompt/`](../../src/API/Infrastructure/Agents/Modes/Prompt/) + contributor registration in [`AgentsExtensions.cs`](../../src/API/Infrastructure/Agents/AgentsExtensions.cs).

For the full prompt-specific walkthrough (XML sections, mode strategies, compose decorators), see [Prompt pipeline](prompt-pipeline.md).

## What it is

A **contributor** is a small, focused class that:

1. Implements a shared interface with one method (e.g. `Contribute(context, document)`)
2. Reads **input context** and writes into a **shared mutable document**
3. Runs in a **pipeline** that invokes every contributor in registration order
4. Can **no-op** when its conditions are not met (guard clauses at the top)

The pipeline does not know what each contributor does — it only loops and calls `Contribute`. Contributors do not call each other.

## How Orchi applies it

```
AgentPromptComposer.Compose(session, userContent)
    → build PromptBuildContext
    → PromptSectionPipeline.Build(context)
        → foreach IPromptSectionContributor
            → contributor.Contribute(context, document)
    → OrchiPromptRenderer.Render(document)
```

Core types:

| Type | Role |
|------|------|
| `IPromptSectionContributor` | One method: `Contribute(PromptBuildContext, OrchiPromptDocument)` |
| `PromptSectionPipeline` | Creates empty document, runs all contributors, returns document |
| `PromptBuildContext` | Read-only inputs for this turn (mode, workspace, user message, plan path, parent chat) |
| `OrchiPromptDocument` | Mutable sections — `Identity`, `Rules`, `Context`, `Tools`, `Task`, `Message` |

### Shared sections vs exclusive sections

Some sections accept **multiple writers** via append helpers:

```csharp
document.AppendRules("Focus on <message>.");
document.AppendContext($"Workspace: {context.WorkspacePath}");
```

Others are **owned by one writer** — assign directly:

```csharp
document.Message = context.UserContent;
document.Task = taskText;
```

Use `Append*` when several contributors may add to the same section. Use direct assignment when one contributor owns the slot.

### Registered contributors (order matters)

Contributors run in DI registration order in `AgentsExtensions.cs`:

| Contributor | Section(s) | When |
|-------------|------------|------|
| `ModeSectionContributor` | identity, rules, context, tools | Delegates to `IAgentModeStrategy` for the chat mode |
| `SessionContextContributor` | context | Every turn — workspace path |
| `ReviewDiffContributor` | context | Review mode with a review plan file — git diff |
| `SessionTaskContributor` | task | When `PlanFilePath` is set |
| `ParentChatContributor` | context | When `ParentChatId` is set |
| `GlobalRulesContributor` | rules | Every turn — meta-rule about `<message>` |
| `MessageContributor` | message | Every turn — raw user content (**register last**) |

`MessageContributor` is always last so user content lands in `<message>` after all instruction sections are filled.

## Contributor vs other extension points

| Need | Use | Why |
|------|-----|-----|
| One more slice of prompt content (rules, context, task) | `IPromptSectionContributor` | Independent writer; no changes to siblings |
| A whole chat mode personality (identity + rules + templates) | `IAgentModeStrategy` | Cohesive bundle; reached via `ModeSectionContributor` |
| Logging or debug around compose | `Decorate<IAgentPromptComposer, …>` | Cross-cutting wrapper — same idea as CQRS behaviours |
| New XML tag or section order | `OrchiPromptDocument` + `OrchiPromptRenderer` | Output shape, not content |

**Rule of thumb:** If it is "add this paragraph when X is true," use a contributor. If it is "define what orchestration mode means," use a mode strategy. If it is "log prompt length," use a compose decorator.

See [Decorator](decorator.md) for the wrapper chain pattern and [Prompt pipeline](prompt-pipeline.md) for how contributors fit with mode strategies and XML rendering.

## Adding a contributor

1. Create a class in `src/API/Infrastructure/Agents/Modes/Prompt/` implementing `IPromptSectionContributor`
2. Guard early — return without writing when your conditions are not met
3. Write via `AppendRules` / `AppendContext` or direct property assignment
4. Register in `AgentsExtensions.cs` with `services.AddSingleton<IPromptSectionContributor, YourContributor>()`
5. Place registration **before** `MessageContributor` unless you intentionally need to run after user content (rare)
6. Add pipeline tests in `tests/Orchi.Api.Tests/Infrastructure/Agents/Modes/Prompt/`

Minimal example:

```csharp
public sealed class ParentChatContributor : IPromptSectionContributor
{
    public void Contribute(PromptBuildContext context, OrchiPromptDocument document)
    {
        if (context.ParentChatId is null)
        {
            return;
        }

        document.AppendContext($"Parent chat: {context.ParentChatId}");
    }
}
```

## Testing

Unit tests construct a pipeline with explicit contributor instances — no DI container required. See `PromptTestHelpers.CreatePipeline()` and `PromptSectionPipelineTests`.

Pattern:

1. Build a `PromptBuildContext` with the inputs you care about
2. Call `pipeline.Build(context)`
3. Assert on `OrchiPromptDocument` properties (`Rules`, `Context`, `Task`, `Message`, …)

Test contributors in isolation by passing a single contributor to `new PromptSectionPipeline([contributor])`, or test the full stack via `PromptTestHelpers`.

## FAQ

### Why not one big `ComposePrompt` method?

A monolithic method grows every time you add orchestration, review diffs, parent chats, or global rules. Contributors keep each concern in one file and make order explicit in DI registration.

### Does registration order matter?

Yes. Contributors run sequentially. Context appended by `SessionContextContributor` appears before context from `ParentChatContributor` because that is how they are registered. Put foundational sections early; put `MessageContributor` last.

### Can a contributor skip work?

Yes — use guard clauses and return early. `ReviewDiffContributor` only runs in review mode with a review plan path; `ParentChatContributor` only runs when `ParentChatId` is set.

### Is this the same as CQRS behaviours?

Similar shape (many classes, one pipeline), different mechanics. Behaviours **wrap** a handler and call `innerHandler`. Contributors **mutate a shared document** and never delegate to each other.

## Related docs

- [Prompt pipeline](prompt-pipeline.md) — `<orchi>` XML envelope, mode strategies, compose decorators
- [Decorator](decorator.md) — CQRS behaviour stack and compose decorators
- [Agents](../agents/README.md) — message flow and agent modes
