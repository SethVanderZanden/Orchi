# Agent CLI command suite

## Dummy section (start here)

Finding a CLI on a developer machine is like **finding the right tool in a shared workshop**.

- Every chef (Cursor, Codex, Claude…) brings different tools.
- The workshop still has one **tool rack** (PATH / PATHEXT) and one **checkout clerk** (shared resolver).
- Each chef only labels their own drawers (install folders + how to unwrap an npm box).

```
Configured name "codex"
        │
        ▼
 AgentCliCommandResolver  ←── shared rack / PATHEXT rules
        │
        ├── IAgentCliInstallLayout (Cursor drawers)
        └── IAgentCliInstallLayout (Codex drawers)
        │
        ▼
 AgentCliLaunchSpec  →  AgentCliProcessStart  →  Process.Start
```

**Aha:** adding Claude later should mean a new drawer label (`IAgentCliInstallLayout`), not a second copy of PATH search.

| Workshop | Orchi | T3 Code (open source) |
|----------|-------|------------------------|
| Shared rack rules | `AgentCliCommandResolver` | `@t3tools/shared/shell` (`resolveCommandPath`) |
| Chef drawers | `IAgentCliInstallLayout` | Provider `Drivers/*` (e.g. `ClaudeExecutable`) |
| Checkout / spawn | `AgentCliProcessStart` | `resolveSpawnCommand` (+ `shell: true` for `.cmd`) |
| Order ticket | `IAgentAdapter` | Provider adapter / session runtime |

Everything below is the same idea with C# types and Windows quirks.

---

## Why this exists

Cursor and Codex both need Windows-aware CLI discovery. Duplicating PATH/PATHEXT logic per agent caused the Codex fresh-install bug: Orchi picked the extensionless npm shim (`nodejs\codex`) that `CreateProcess` cannot run.

T3 Code already solved this once for every provider. Orchi now mirrors that suite without adopting Effect/TypeScript.

## Patterns in play

| Pattern | Role |
|---------|------|
| **Strategy** | `IAgentCliInstallLayout` — per-agent install dirs + npm/native unwrap |
| **Template / shared service** | `AgentCliCommandResolver` — one PATH/PATHEXT algorithm for all agents |
| **Adapter** (unchanged) | `IAgentAdapter` — spawn + parse events for one agent |

This matches Orchi’s existing Strategy + Factory style (`IAgentModeStrategy`, `IScriptActionStrategy`) and T3’s split between shared shell helpers and per-provider drivers.

## Resolution order

1. Absolute `Executable` path (if rooted and present)
2. Layout preferred install directories + `TryResolveBundle` (native `.exe` / `node` + entry script)
3. Merged user + machine PATH with `PATHEXT` (`.exe` preferred over `.cmd`; **extensionless ignored on Windows**)
4. Layout Windows fallbacks (`%LOCALAPPDATA%\cursor-agent`, `%APPDATA%\npm`, `%ProgramFiles%\nodejs`, …)

## Spawn rules

`AgentCliProcessStart.Create`:

- **Direct / node-bundle** → `FileName` + `ArgumentList`, `UseShellExecute=false`
- **`.cmd` / `.bat`** → `ComSpec` (`cmd.exe`) with `/d /s /c` and a quoted command line (redirected IO still works)

Same constraint T3 documents: Node/`CreateProcess` cannot execute launcher scripts without a shell.

## Adding a new agent CLI

1. Implement `IAgentCliInstallLayout` (candidate names, install dirs, `TryResolveBundle`)
2. Thin `XxxAgentExecutableResolver` that calls `AgentCliCommandResolver.Resolve(...)`
3. Adapter uses `AgentCliProcessStart.Create(launch, workspace, args)`
4. Unit-test the layout + shared resolver with `IExecutableEnvironment` fakes

Do **not** copy PATH search into the new agent folder.

## Further reading

- [T3 Code providers](https://github.com/pingdotgg/t3code) — `packages/shared/src/shell.ts`, `apps/server/src/provider/Drivers/`
- [Adapters](../agents/adapters.md)
- [Cursor CLI](../agents/cursor-cli.md)
- [Codex CLI](../agents/codex.md)
- [Event scripting](event-scripting.md) — another Strategy + Factory suite in Orchi
