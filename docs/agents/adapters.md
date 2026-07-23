# Adding agent adapters

## Dummy section (start here)

An adapter is a **power adapter** for travel. Your laptop (Orchi API) always expects the same plug shape (`IAgentAdapter`). Each country (Cursor, Codex, Claude) needs a different physical plug, but the hotel room outlet never changes.

Add a new adapter = ship a new plug, register it in the factory, expose the agent id when creating chats.

**Orchi translation:**

| Travel adapter | Code |
|----------------|------|
| Universal outlet | `IAgentAdapter` |
| Plug inventory | `IAgentAdapterFactory` / `AgentAdapterFactory` |
| US plug | `CursorAgentAdapter` (`AgentId => "cursor"`) |
| Future EU plug | e.g. `CodexAgentAdapter` (`AgentId => "codex"`) |

---

## Contract

```csharp
public interface IAgentAdapter
{
    string AgentId { get; }

    IAsyncEnumerable<AgentEvent> SendMessageAsync(
        ChatSession session,
        string prompt,
        IReadOnlyList<string> extraCliArgs,
        CancellationToken cancellationToken);
}
```

CLI adapters delegate to **`AgentCliTurnProcessor`** (template method) with a per-agent **`IAgentCliProcessorProfile`** (strategy bundle):

| Strategy | Interface | Example |
|----------|-----------|---------|
| argv builder | `IAgentCliArgumentBuilder` | `CodexCliArgumentBuilder` |
| stdout parser | `IAgentStreamLineParser` | `CodexNdjsonParser` |
| executable resolve | `IAgentLaunchResolver` | `CodexAgentLaunchResolverService` |
| profile factory | `IAgentCliProcessorFactory` | `AgentCliProcessorFactory` |

### CLI executable discovery (keep this boring)

Shared helpers live under `Infrastructure/Agents/Cli/`:

- **`AgentCliPathLocator`** — PATH + `PATHEXT` command lookup (`.exe` / `.cmd`), skip extensionless bash shims on Windows
- **`IExecutableEnvironment`** — testable filesystem/PATH seam
- **`AgentLaunchSpec`** + **`AgentProcessStartInfoBuilder`** — spawn (optional `cmd.exe /c` for `.cmd`)

Per-agent resolvers (`*AgentExecutableResolver`) only add product-specific bits:

| Agent | Order |
|-------|--------|
| **Codex (default for new CLIs)** | absolute path → **PATH shortcut** → known installer dirs → npm `node`+`codex.js` last resort. Never dig into nested `vendor/` binaries. |
| **Cursor** | absolute path → local `cursor-agent` **node bundle** (avoids bad `.cmd` shims) → PATH → fallbacks |

When adding a new CLI harness, copy the **Codex** PATH-first pattern and call `AgentCliPathLocator` — do not invent a new PATH walker.

Register one `IAgentCliProcessorProfile` per agent in `AgentsExtensions.AddOrchiAgents()`. The adapter itself stays a thin `IAgentAdapter` facade.

Implementations:

1. Spawn the agent CLI or HTTP client for **one user turn**
2. Stream normalized **`AgentEvent`** values (text delta, tool started/completed, completed, error)
3. Respect **`CancellationToken`** — kill the child process on cancel
4. Store external session ids on **`ChatSession`** when the agent supports resume

## Event types

Defined in `src/API/Infrastructure/Agents/AgentEvents.cs`:

| Event | Purpose |
|-------|---------|
| `AgentTextDeltaEvent` | Incremental assistant text |
| `AgentToolStartedEvent` / `AgentToolCompletedEvent` | Tool execution rows for UI markers |
| `AgentStatusEvent` | Processing phase (e.g. `"processing"`) |
| `AgentCompletedEvent` | Turn finished; optional full text + external session id |
| `AgentErrorEvent` | User-visible failure |

The chat `SendMessage` endpoint maps these to SSE — see [chat-streaming.md](../frontend/chat-streaming.md).

## Registration

1. Create `Infrastructure/Agents/{Name}/{Name}AgentAdapter.cs`
2. Add options class + `appsettings.json` section if needed
3. Register in `AgentsExtensions.AddOrchiAgents()`:

```csharp
services.AddSingleton<IAgentAdapter, CursorAgentAdapter>();
services.AddSingleton<IAgentAdapterFactory, AgentAdapterFactory>();
```

`AgentAdapterFactory` resolves by `AgentId` string from `POST /chats`.

## Adding Claude (future)

1. Implement `IAgentStreamLineParser` for the agent's NDJSON stdout
2. Implement `IAgentCliArgumentBuilder` for argv (model, resume, config overrides)
3. Implement `IAgentLaunchResolver` wrapping a thin `*ExecutableResolver` that uses **`AgentCliPathLocator`** (PATH-first like Codex)
4. Register `{Name}AgentCliProcessorProfile` as `IAgentCliProcessorProfile`
5. Implement `IAgentAdapter` as a thin wrapper over `AgentCliTurnProcessor`
6. Add config section (`Agents:Claude`, etc.)
7. Allow `agent: "claude"` in `CreateChatRequest` (validation already checks factory)
8. Add parser unit tests with fixture stdout (no real CLI in CI)
9. Document CLI flags and event mapping in a new `docs/agents/{name}.md`

Codex is implemented — see [codex.md](codex.md).

## Session manager responsibilities

`AgentSessionManager` stays agent-agnostic:

- Creates / closes sessions
- Appends user and assistant messages
- Calls `adapter.SendMessageAsync`
- Tracks `RunningProcess` and `RunCts` for cleanup
- `CloseAllSessions()` on API shutdown

Adapters should not own the message list — only emit events.

## Further reading

- [Agent overview](README.md)
- [Cursor CLI reference](cursor-cli.md)
