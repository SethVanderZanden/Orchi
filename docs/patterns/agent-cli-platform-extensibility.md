# Agent CLI platform extensibility

## Dummy section (start here)

Think of Orchi’s CLI finder as a **shipping dock** that must unload the same cargo (Cursor, Codex, …) in different countries.

- The **country** is the OS (Windows, macOS, Linux) — auto-detected from the running process.
- The **carrier** is how the tool was installed (npm global, Homebrew, native installer, Volta, …) — auto-detected from the path we find.
- The **manifest** is a small table of “if country X + carrier Y, look in these bays / unwrap this box.”

```
Detect country (OS)
        │
        ▼
Search PATH + known bays for that country
        │
        ▼
Find a binary / shim
        │
        ▼
Detect carrier (install kind) from that path
        │
        ▼
Unwrap to a spawnable launch (native exe, node+script, cmd wrapper, …)
```

**Aha:** Orchi should never ask the user “are you on Mac?” — it detects the OS, then detects the install from the filesystem, then picks the right unwrap rule.

| Dock analogy | Orchi type |
|--------------|------------|
| Country | `AgentCliHostPlatform` (Windows / MacOS / Linux) |
| Carrier | `AgentCliInstallKind` (NpmGlobal, Homebrew, Native, …) |
| Shared customs rules | `AgentCliCommandResolver` |
| Per-chef cargo notes | `IAgentCliInstallLayout` |
| Spawnable crate | `AgentCliLaunchSpec` |

Everything below is the phased plan and the hooks that make adding OSes cheap.

---

## Goals

1. **Auto-detect OS** from `OperatingSystem` / `RuntimeInformation` — no config switch.
2. **Auto-detect install kind** from the resolved path (and nearby files), not from user prompts.
3. Keep **one PATH algorithm**; only tables of directories and unwrap rules grow per OS/agent.
4. Stay consistent with Orchi’s Strategy + Adapter style; use other open-source hosts only as **evidence**, not as a port target.

## Non-goals (for this plan)

- Porting another product’s CLI stack into Orchi.
- Auto-installing missing CLIs.
- WSL-as-a-separate-platform (treat as Linux inside WSL; document later if GUI↔WSL bridging is needed).

## Detection model

### Host platform (country)

```csharp
enum AgentCliHostPlatform { Windows, MacOS, Linux, Unknown }
```

Resolved once from the environment:

| Check | Platform |
|-------|----------|
| `OperatingSystem.IsWindows()` | Windows |
| `OperatingSystem.IsMacOS()` | MacOS |
| `OperatingSystem.IsLinux()` | Linux |
| else | Unknown (PATH-only fallback) |

Architecture (x64 vs arm64) is read the same way (`RuntimeInformation.OSArchitecture`) when selecting npm platform packages (`@openai/codex-darwin-arm64`, …).

### Install kind (carrier)

After a candidate path is found, classify it:

| Signal | Kind |
|--------|------|
| Path under `…/npm/…`, `node_modules/@openai/…`, `node_modules/@anthropic-ai/…` | `NpmGlobal` |
| Path under `/opt/homebrew/…` or `/usr/local/Cellar/…` / Homebrew bin | `Homebrew` |
| Path under `%LOCALAPPDATA%\cursor-agent\…` or vendor native layout | `NativeInstaller` |
| Path under `…\Volta\bin` / `~/.volta/bin` | `Volta` |
| Otherwise | `Unknown` (still spawnable if PATH resolution succeeded) |

Install kind drives **unwrap preference**, not whether we accept the binary:

1. Prefer native binary next to the package (`codex.exe` / `codex` in platform package)
2. Else node + entry script (`node` + `bin/codex.js`)
3. Else Windows `.cmd` via `cmd.exe`
4. Else direct PATH binary (Unix)

## Shared directory tables (by platform)

These are **known bays**, always searched in addition to PATH. They do not require the user to set config.

| Platform | Shared known dirs (examples) |
|----------|------------------------------|
| Windows | `%APPDATA%\npm`, `%LOCALAPPDATA%\Programs\nodejs`, `%ProgramFiles%\nodejs`, `%LOCALAPPDATA%\Volta\bin`, `%LOCALAPPDATA%\pnpm` |
| macOS | `/opt/homebrew/bin`, `/usr/local/bin`, `~/.npm-global/bin`, `~/Library/pnpm`, `~/.volta/bin` |
| Linux | `/usr/bin`, `/usr/local/bin`, `~/.local/bin`, `~/.npm-global/bin`, `~/.volta/bin` |

Agent layouts **add** agent-specific dirs (e.g. Cursor `~/.local/share/cursor-agent` or `%LOCALAPPDATA%\cursor-agent`) on top of the shared table.

## Per-agent platform packages (auto from OS + arch)

Example Codex native unwrap matrix (same idea as npm optional deps):

| Platform | Arch | Package / path hint |
|----------|------|---------------------|
| Windows | X64 | `@openai/codex-win32-x64` → `…/codex.exe` |
| Windows | Arm64 | `@openai/codex-win32-arm64` |
| macOS | X64 | `@openai/codex-darwin-x64` → `…/codex` |
| macOS | Arm64 | `@openai/codex-darwin-arm64` |
| Linux | X64 / Arm64 | `@openai/codex-linux-x64` / `linux-arm64` |

Cursor keeps its versioned `node` + `index.js` bundle layout; paths differ by OS but the **strategy method** stays `TryResolveBundle`.

## Extensibility checklist (new OS or new agent)

### New OS (rare)

1. Add value to `AgentCliHostPlatform` if needed.
2. Extend `AgentCliKnownDirectories.For(platform, env)` with that OS’s known bins.
3. Add PATH separator / executable rules only if they differ (today: Windows vs Unix is enough).
4. Add platform rows to each agent’s native package table.
5. Tests with `IExecutableEnvironment` fakes (`HostPlatform = MacOS`, fake files).

### New agent (common)

1. Implement `IAgentCliInstallLayout` (candidate names + agent dirs + `TryResolveBundle`).
2. Reuse shared known dirs + resolver + process start.
3. Document install matrix in `docs/agents/{name}.md`.

**Do not** fork PATH search per agent or per OS.

## Phased rollout

### Phase 0 — Foundation (done in tree)

- `AgentCliHostPlatform` + arch detection on `IExecutableEnvironment`
- `AgentCliInstallKind` + path classifier (`AgentCliHostDetector`)
- Shared `AgentCliKnownDirectories` (Windows / macOS / Linux)
- OS-agnostic `GetFallbackPaths` on layouts
- Codex native package matrix for win/mac/linux × x64/arm64
- Cursor preferred dirs for Windows + macOS/Linux home layouts
- Plan documented here; suite doc links here

### Phase 1 — macOS / Linux install bays (done in tree)

- Known dirs for Homebrew / npm-global / `~/.local/bin` / linuxbrew
- Cursor macOS/Linux preferred dirs (marked **best-effort** until verified on a real install)
- Codex darwin/linux platform package unwrap matrix
- Fake-filesystem tests for known dirs + install-kind classification

### Phase 2 — GUI-app PATH parity (done in tree)

- Soft-fail login-shell PATH probe (`$SHELL -l`) on macOS/Linux
- Soft-fail `launchctl getenv PATH` on macOS
- Merged into `ExecutableEnvironment.GetPathDirectories()`; probes never block resolution
- Windows User + Machine PATH merge unchanged

### Phase 3 — Observability (done in tree)

- `ResolveResult` stamps `HostPlatform`, `InstallKind`, `LaunchKind`, `SearchedPaths`
- Adapters log path + platform + install + launch at Debug on start
- Settings “CLI probe” UI remains optional later

## Mac path caveat

Cursor/Codex Unix install directories under `~/.local/share/...` and similar are **best-effort guesses**. Official installer layouts can differ by version. Prefer PATH / Homebrew / npm-global when present; set `Agents:*:Executable` only if auto-detect misses.
## Success criteria

- Adding Linux support for an existing agent = table rows + tests, not a new resolver class
- Fresh Mac Homebrew `codex` and Windows npm `codex.cmd` both resolve without user path config
- Unknown OS still works via PATH-only search
- Docs stay Dummy-first and point agents at this plan

## Further reading

- [Agent CLI command suite](agent-cli-command-suite.md) — current shared resolver
- [Adapters](../agents/adapters.md)
