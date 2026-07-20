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

| Workshop | Orchi |
|----------|-------|
| Shared rack rules | `AgentCliCommandResolver` |
| Chef drawers | `IAgentCliInstallLayout` |
| Checkout / spawn | `AgentCliProcessStart` |
| Order ticket | `IAgentAdapter` |

Everything below is the same idea with C# types and Windows quirks.

---

## Why this exists

Cursor and Codex both need Windows-aware CLI discovery. Duplicating PATH/PATHEXT logic per agent caused the Codex fresh-install bug: Orchi picked the extensionless npm shim (`nodejs\codex`) that `CreateProcess` cannot run.

We consolidated discovery into one suite that fits Orchi’s Strategy + Adapter patterns (`IAgentModeStrategy`, `IScriptActionStrategy`, `IAgentAdapter`).

## Patterns in play

| Pattern | Role |
|---------|------|
| **Strategy** | `IAgentCliInstallLayout` — per-agent install dirs + npm/native unwrap |
| **Template / shared service** | `AgentCliCommandResolver` — one PATH/PATHEXT algorithm for all agents |
| **Adapter** (unchanged) | `IAgentAdapter` — spawn + parse events for one agent |

## Resolution order

1. Absolute `Executable` path (if rooted and present) → unwrap once
2. Probe **known + agent install dirs** for native / node bundles (before PATH shims)
3. Search PATH + known dirs for a spawnable file (Windows: `PATHEXT`, no extensionless; Unix: extensionless)
4. Layout fallbacks → unwrap once

Each success stamps `HostPlatform`, `InstallKind`, `LaunchKind` on `ResolveResult` (logged at Debug by adapters).

Host OS, install kind (npm / Homebrew / native / Volta), and optional login-shell PATH enrichment are **auto-detected** — see [platform extensibility](agent-cli-platform-extensibility.md#dummy-section-start-here).

macOS/Linux Cursor home dirs are **best-effort** until verified against a real CLI install.

## Spawn rules

`AgentCliProcessStart.Create`:

- **Direct / node-bundle** → `FileName` + `ArgumentList`, `UseShellExecute=false`
- **`.cmd` / `.bat`** (Windows only) → `ComSpec` (`cmd.exe`) with `/d /s /c` and a quoted command line (redirected IO still works)

Windows constraint: `CreateProcess` / `UseShellExecute=false` cannot run `.cmd`/`.bat` launcher scripts directly, so Orchi wraps them with `cmd.exe` while keeping stdout/stderr redirected.

## Adding a new agent CLI

1. Implement `IAgentCliInstallLayout` (candidate names, install dirs, `TryResolveBundle`)
2. Thin `XxxAgentExecutableResolver` that calls `AgentCliCommandResolver.Resolve(...)`
3. Adapter uses `AgentCliProcessStart.Create(launch, workspace, args)`
4. Unit-test the layout + shared resolver with `IExecutableEnvironment` fakes (`HostPlatform` / `HostArchitecture`)

Do **not** copy PATH search into the new agent folder. Do **not** add an “OS mode” setting — detect it.

## Further reading

- [Platform extensibility plan](agent-cli-platform-extensibility.md) — Mac/Linux/Windows roadmap + install-kind detection
- [Adapters](../agents/adapters.md)
- [Cursor CLI](../agents/cursor-cli.md)
- [Codex CLI](../agents/codex.md)
- [Event scripting](event-scripting.md) — another Strategy + Factory suite in Orchi
