# SharedContext Integration

## Dummy section (start here)

SharedContext plugs into the existing agent pipeline like a **sidecar** — the chat front desk (`AgentSessionManager`) still runs turns, but now asks the shared notebook for context before mailing each letter.

```
Chat create / message
  → AgentSessionManager
  → IChatModeStrategy.PrepareTurnAsync
  → AgentPromptComposer → IPromptBuilder + IModeRuntime
  → CursorAgentAdapter
  → Turn complete → ISessionDistiller + IWorkspaceEventBus
```

---

## Status

**done** (Phase 1–2 wiring) — see [PROGRESS.md](PROGRESS.md)

## DI registration

[`Program.cs`](../../src/API/Program.cs):

```csharp
.AddOrchiSharedContext(builder.Configuration)
.AddOrchiAgents(builder.Configuration)
```

[`SharedContextExtensions.cs`](../../src/SharedContext/SharedContextExtensions.cs) registers all SharedContext services.

## Agent changes

| File | Change |
|------|--------|
| `AgentTurnRequest.cs` | `StablePrefix`, `DynamicContext`, `CliProfileKind` |
| `AgentPromptComposer.cs` | Bridges `ChatSession` → `IPromptBuilder` |
| `*ModeStrategy.cs` | Thin wrappers around composer |
| `AgentSessionManager.cs` | Mode resume policy, events, session distiller |
| `ChatSession.cs` | `PreviousModeKey`, `ModeChangedAt` |

## Configuration

`appsettings.json` → `SharedContext` section (connection string, index limits, retrieval top-K).

## Deferred (Phase 4)

- Cross-chat summary propagation (orchestrator ← child chats).
- Desktop context panel.
