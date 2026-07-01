# Prompt composition

## Dummy section (start here)

Think of every agent request as a **form letter**. The top of the page is company letterhead — logo, address, boilerplate rules. That part is printed once and reused on thousands of letters. The bottom is today's message — different every time.

LLM providers can do something similar: if the **beginning** of a prompt is byte-identical across requests, they cache that work and charge less (and respond faster). Change one line in the letterhead and the cache misses.

```
┌─────────────────────────────────────┐
│  STABLE PREFIX (letterhead)         │  ← same every turn in a mode
│  mode instructions, project rules   │
├─────────────────────────────────────┤
│  DYNAMIC CONTEXT (today's letter)   │  ← changes every turn
│  user message, plan, errors, diffs  │
└─────────────────────────────────────┘
```

**The aha:** Put everything that rarely changes **first**, everything that changes often **last**.

**Orchi translation:**

| Letter analogy | Orchi today |
|----------------|-------------|
| Letterhead | `ModeInstructions.*` prepended in mode strategies |
| Today's letter | User content after `\n\n---\n\n` |
| Mailing the letter | `AgentTurnRequest.PreparedPrompt` → `CursorAgentAdapter` → CLI positional arg |
| Filing cabinet for continuity | `--resume` + `ChatSession.ExternalSessionId` (Cursor-owned) plus Orchi-owned transcript replay in the dynamic suffix |

Everything below is the same idea with file paths, current gaps, and design rules for when we add provider caching.

---

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

---

## How Orchi builds prompts today

End-to-end flow:

```
Desktop (raw user content)
  → AgentSessionManager.ExecuteTurnAsync
  → IChatModeStrategy.PrepareTurn
  → AgentTurnRequest(PreparedPrompt, ExtraCliArgs)
  → CursorAgentAdapter.SendMessageAsync
  → agent ... [--resume] "{preparedPrompt}"
```

`AgentTurnRequest` is a flat string today — no structural split between stable and dynamic parts:

```3:7:src/API/Infrastructure/Agents/Modes/AgentTurnRequest.cs
public sealed record AgentTurnRequest(string PreparedPrompt, IReadOnlyList<string> ExtraCliArgs)
{
    public static AgentTurnRequest FromUserContent(string userContent) =>
        new(userContent, []);
}
```

Mode strategies concatenate instructions, optional context, a `---` delimiter, and user content. Constants live in `src/API/Infrastructure/Agents/Modes/Strategies/ModeInstructions.cs`.

### Per-mode composition

| Mode | Stable prefix | Dynamic suffix | Extra CLI args |
|------|---------------|----------------|----------------|
| `plan` | `ModeInstructions.Plan` | User content | `--mode=plan` |
| `orchestrate` | `ModeInstructions.Orchestrate` | User content | `--mode=plan` |
| `implement` | `ModeInstructions.Implement` | Attached plan markdown + user content | *(none)* |
| `goal` | `ModeInstructions.Goal` or `GoalCheckIn` | User content or check-in payload | `--mode=plan` or `--mode=ask` |

**Plan** (typical pattern):

```csharp
string prepared = $"{ModeInstructions.Plan}\n\n---\n\n{userContent.Trim()}";
```

**Implement** injects the full plan into the dynamic section every turn (correct placement — plan content changes, mode instructions do not):

```csharp
$"""
{ModeInstructions.Implement}

## Attached plan

{planContent.Value}

---

{userContent.Trim()}
"""
```

**Goal** switches instruction sets between normal turns and check-ins — this changes the stable prefix between turn types:

```csharp
bool isCheckIn = userContent.StartsWith("[goal-check-in]", StringComparison.Ordinal);
string instructions = isCheckIn ? ModeInstructions.GoalCheckIn : ModeInstructions.Goal;
string prepared = $"{instructions}\n\n---\n\n{userContent.Trim()}";
```

### Recommendation layer mapping

| Recommendation layer | Orchi today | Future target |
|---------------------|-------------|---------------|
| System instructions | `ModeInstructions.*` prepended in strategies | Same — kept stable per mode |
| Tool definitions | Cursor CLI owns tools (Orchi parses inbound NDJSON only) | Document boundary; adapter passes flags only |
| Coding standards / project rules | Injected via `IPromptBuilder` | `.cursor/rules`, `AGENTS.md` in stable prefix |
| Repo architecture summary | Partial (file summaries) | Context store + retrieval in dynamic suffix |
| Current task / user message | After `---` | Always in dynamic suffix |
| Attached plan / check-ins | Implement + Goal modes | Dynamic suffix only |

---

## Current gaps

1. **Prefix instability in Goal mode** — `Goal` vs `GoalCheckIn` instructions swap the stable prefix between turn types.
2. **No Orchi-side caching hooks** — stable prefix is structured but not fingerprinted for provider cache markers yet.
3. **Session summary maturity** — rolling distillation supplements but does not yet fully replace transcript replay.
4. **Embedding search** — keyword retrieval only; true semantic search is Phase 3.

### Resolved (SharedContext)

- `AgentTurnRequest` now has explicit `StablePrefix` + `DynamicContext` (see `src/API/Infrastructure/Agents/Modes/AgentTurnRequest.cs`).
- Project rules injected via `IPromptBuilder` / `ProjectRulesLoader`.
- History replay moved into `IPromptBuilder`; see [prompt-builder.md](../context/prompt-builder.md).
- Mode transition markers and conditional `--resume` preservation via `IModeRuntime`.

---

## SharedContext integration

Prompt assembly is centralized in `Orchi.SharedContext`:

```
IChatModeStrategy.PrepareTurnAsync
  → AgentPromptComposer
  → IPromptBuilder.BuildTurnAsync
  → AgentTurnRequest(StablePrefix, DynamicContext, ExtraCliArgs, CliProfileKind)
```

Full details: [docs/context/README.md](../context/README.md#dummy-section-start-here).

---

## Design rules for future changes

When editing prompt assembly code, follow these rules:

1. **Never prepend variable content before stable instructions.** Plans, diffs, errors, timestamps, and session IDs belong after `---`.
2. **Keep `ModeInstructions` constants stable.** Do not embed per-turn or per-session data in mode instruction strings.
3. **Put changing context in the dynamic suffix.** User message, attached plans, check-in payloads, file excerpts, and error output go after the delimiter.
4. **Centralize project rules in a prefix builder.** When adding `.cursor/rules` or `AGENTS.md` content, assemble it once in a shared stable-prefix builder — not scattered across mode strategies.
5. **Preserve per-mode prefix consistency.** Avoid switching instruction sets within a mode unless documented (Goal/GoalCheckIn is a known exception).
6. **Refactor toward explicit fields.** When implementing caching, split `AgentTurnRequest` into `StablePrefix` + `DynamicContext` before concatenating for the CLI.

---

## Boundaries (what Orchi does not control)

- **Cursor CLI internal system prompt** — Orchi passes one positional prompt argument; the CLI may add its own layers.
- **Tool definitions** — owned by the Cursor runtime, not assembled by Orchi.
- **Provider cache TTL and hit rates** — Anthropic, OpenAI, and others have their own caching semantics.
- **`--resume` session behavior** — Orchi preserves `ExternalSessionId` when CLI profile unchanged; session summaries supplement transcript replay. See [mode-runtime.md](../context/mode-runtime.md).

---

## Future implementation

Remaining work for provider caching:

1. Fingerprint stable prefix hash in `IPromptBuilder` for cache markers.
2. Shrink transcript replay as session summaries mature.
3. Map structured parts to per-provider adapter capabilities when adding non-Cursor agents.

Implemented: explicit `StablePrefix` / `DynamicContext` on `AgentTurnRequest`; centralized `IPromptBuilder`. See [prompt-builder.md](../context/prompt-builder.md).

---

## Further reading

- [Agent adapters overview](README.md)
- [Cursor CLI integration](cursor-cli.md) — spawn args, `--resume`, NDJSON
- [Chat modes](README.md#chat-modes-vs-agents) — mode strategy overview
- Project skill: `.cursor/skills/orchi-agent-prompts/SKILL.md` — enforcement checklist when editing prompt code
