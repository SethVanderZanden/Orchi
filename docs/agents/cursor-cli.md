# Cursor CLI integration

## Dummy section (start here)

The Cursor CLI is like a **walkie-talkie** to a remote coworker. You press talk (`agent -p "..."`), they reply in short bursts (NDJSON lines on stdout), and you only write down the **new** words — not every partial repeat of the same sentence.

Orchi listens on that walkie-talkie, filters duplicate partial messages, and rebroadcasts clean events to the desktop over SSE.

**Orchi translation:**

| Walkie-talkie | Orchi |
|---------------|-------|
| Press talk | `CursorAgentAdapter` spawns `agent` process |
| Bursts of speech | NDJSON lines (`type: assistant`, `tool_call`, `result`) |
| Ignoring echo | Skip `assistant` events with `model_call_id` |
| "I'm done" | `result` event → store session id for `--resume` |
| Loudspeaker to UI | `ChatSseWriter` → `token`, `tool`, `done` SSE events |

---

## Prerequisites

1. Install the [Cursor CLI](https://cursor.com/docs/cli/overview) (`agent` on PATH in your terminal)
2. Authenticate (`agent login` or equivalent per Cursor docs)
3. Run the Orchi API on the **same machine** as the CLI

### API process PATH (Windows)

Your terminal may find `agent` while the Orchi API cannot. The API is started by `dotnet run`, Visual Studio, or another host that often **does not inherit your user PATH** (including `%LOCALAPPDATA%\cursor-agent` added at install time).

Orchi resolves the executable before spawning:

1. Absolute path from `Agents:Cursor:Executable` (if set and file exists)
2. Search merged user + machine PATH with `PATHEXT` (`.exe`, `.cmd`, …)
3. Windows fallback: `%LOCALAPPDATA%\cursor-agent\agent.exe` (then `cursor-agent.exe`, `.cmd` shims)

Prefer **`agent.exe`** over `.cmd`/`.ps1` wrappers when multiple candidates exist.

**If spawn still fails:**

- Restart the Orchi API after installing the Cursor CLI
- Set a full path in `appsettings.json`:

```json
"Agents": {
  "Cursor": {
    "Executable": "C:\\Users\\<you>\\AppData\\Local\\cursor-agent\\agent.exe"
  }
}
```

- Optional: add custom search directories via `AdditionalSearchPaths`

Implementation: `src/API/Infrastructure/Agents/Cursor/CursorAgentExecutableResolver.cs`

## Spawn command

Orchi runs one CLI process **per user message**, using `--resume` for multi-turn continuity:

```bash
agent -p --force --trust --workspace "{workspace}" \
  --output-format stream-json --stream-partial-output \
  [--resume {cursorSessionId}] \
  "{userMessage}"
```

Configured via `Agents:Cursor` in `appsettings.json` (executable name or full path, default args, optional `AdditionalSearchPaths`).

## NDJSON event mapping

Parser: `src/API/Infrastructure/Agents/Cursor/CursorNdjsonParser.cs`

| Cursor `type` | Condition | Orchi `AgentEvent` |
|---------------|-----------|-------------------|
| `assistant` | has `timestamp_ms`, **no** `model_call_id` | `AgentTextDeltaEvent` |
| `assistant` | has `model_call_id` | **Skip** (buffered duplicate from partial output) |
| `tool_call` | started | `AgentToolStartedEvent` |
| `tool_call` | completed | `AgentToolCompletedEvent` |
| `result` | — | `AgentCompletedEvent` + store external session id |
| parse / process error | — | `AgentErrorEvent` |

### Partial output

With `--stream-partial-output`, Cursor emits multiple `assistant` shapes. Consumers must ignore lines that include `model_call_id` and only accept incremental deltas with `timestamp_ms`. See [Cursor forum discussion](https://forum.cursor.com/t/stream-partial-output-assistant-events-have-multiple-undocumented-forms-how-should-consumers-parse-them/156289).

Official reference: [Cursor CLI output format](https://cursor.com/docs/cli/reference/output-format).

## Session resume

When a `result` event includes a session identifier, Orchi stores it on `ChatSession.ExternalSessionId` and passes `--resume` on the next message. This gives multi-turn conversations without keeping a long-lived CLI process.

## Error handling

| Situation | Behaviour |
|-----------|-----------|
| CLI not found by API process | `AgentErrorEvent` with searched paths + restart/config hints |
| Non-zero exit code | `AgentErrorEvent` |
| Invalid workspace at create time | `400` validation from `POST /chats` |
| Client disconnects mid-stream | `CancellationToken` cancels CLI |

## Tests

Unit tests:

- `CursorNdjsonParserTests.cs` — NDJSON → `AgentEvent` mapping (partial-output filtering)
- `CursorAgentExecutableResolverTests.cs` — PATH search, Windows fallback, argument deduplication

## Further reading

- [Agent adapters overview](README.md)
- [Adding other adapters](adapters.md)
- [Official Cursor CLI docs](https://cursor.com/docs/cli/overview)
