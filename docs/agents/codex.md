# Codex CLI integration

## Dummy section (start here)

Codex is another **chef in the kitchen**. Cursor already cooks for Orchi; Codex is a second chef with different knives (CLI flags) and a different way of calling out progress (JSONL events).

Orchi still takes one order ticket (`ChatSession`) and expects the same plated dishes (`AgentEvent`). The Codex adapter translates.

```
Desktop  →  AgentSessionManager  →  CodexAgentAdapter  →  codex exec --json
                                         ↑                        ↓
                                         └──── AgentEvent ← JSONL ─┘
```

| Kitchen idea | Orchi |
|--------------|-------|
| Chef | `CodexAgentAdapter` (`AgentId => "codex"`) |
| Recipe card | `--model` + `-c model_context_window=N` + `-c model_reasoning_effort=…` + `-c approval_policy=…` |
| Ticket number | `thread_id` stored as `ExternalSessionId` |
| Plated courses | `AgentTextDeltaEvent`, `AgentToolEvent`, `AgentCompletedEvent` |

**Aha:** one chat picks the chef and the recipe knobs; Orchi turns those into Codex `-c` overrides without you typing the CLI by hand.

Everything below is the same idea with adapters, catalogs, and JSONL.

---

## Prerequisites

1. Install the [OpenAI Codex CLI](https://developers.openai.com/codex/cli) (`codex` on PATH)
   - **Windows (recommended):** run the [PowerShell installer](https://developers.openai.com/codex/cli) from the official docs, or `npm install -g @openai/codex`
   - **macOS / Linux:** `npm install -g @openai/codex` or the curl installer on the same page
2. Authenticate per Codex docs
3. Run the Orchi API on the same machine

### API process PATH (Windows)

Your terminal may find `codex` while the Orchi API cannot — or the API may find a file that **cannot be started**. This is common on Windows after `npm install -g @openai/codex`.

npm global installs place several launchers in the same directory (for example `C:\Program Files\nodejs\` or `%APPDATA%\npm\`):

| File | What it is | Works from a shell? | Works with `Process.Start`? |
|------|------------|---------------------|-----------------------------|
| `codex` | Bash shim (no extension) | Often yes (Git Bash / shell PATH) | **No** — not a Windows executable |
| `codex.cmd` | Windows batch shim | Yes | Yes (via `cmd.exe /c`) |
| `codex.ps1` | PowerShell shim | Yes (PowerShell) | No (unless invoked through PowerShell) |

Codex’s official docs cover [installation](https://developers.openai.com/codex/cli) but do **not** document this shim layout. The behavior is discussed in upstream issues such as [openai/codex#16337](https://github.com/openai/codex/issues/16337) (Windows `spawn` / `.cmd` shim resolution).

Orchi resolves the executable before spawning:

1. Absolute path from `Agents:Codex:Executable` (if set and file exists)
2. **`node.exe` + `@openai/codex/bin/codex.js`** in the npm prefix directory (preferred on Windows — bypasses `.cmd` shims)
3. Search merged user + machine PATH with `PATHEXT` (`.exe`, `.cmd`, …), **skipping extensionless shims on Windows**
4. Windows fallback: `%APPDATA%\npm\codex.cmd`

When only a `.cmd` shim is found, Orchi launches it via `cmd.exe /c` so stdout/stderr redirection still works.

**If spawn still fails:**

- Restart the Orchi API after installing Codex
- Confirm `codex --version` works in a **new** terminal, then restart the API host (Visual Studio, `dotnet run`, etc.) so it picks up PATH changes
- Set a full path in `appsettings.json`:

```json
"Agents": {
  "Codex": {
    "Executable": "C:\\Users\\<you>\\AppData\\Roaming\\npm\\codex.cmd"
  }
}
```

- Optional: add custom search directories via `AdditionalSearchPaths`

Implementation: `src/API/Infrastructure/Agents/Codex/CodexAgentExecutableResolver.cs`

Config (`appsettings.json`):

```json
"Agents": {
  "Codex": {
    "Executable": "codex",
    "DefaultArgs": [
      "--skip-git-repo-check",
      "--sandbox",
      "workspace-write",
      "--ask-for-approval",
      "never"
    ],
    "TimeoutSeconds": 600
  }
}
```

## Spawn command

One process per user turn:

```bash
codex exec --json [--skip-git-repo-check] \
  [--sandbox workspace-write] [--ask-for-approval never] \
  [--model {slug}] \
  [-c model_context_window={tokens}] \
  [-c model_reasoning_effort={effort}] \
  [-c approval_policy={policy}] \
  [resume {threadId}] \
  "{composedPrompt}"
```

Working directory is the chat workspace path. Model, context, reasoning effort, and approval policy come from the chat (mode defaults or composer overrides). Extra `-c` keys are assembled from `ChatSession.CliConfigOverrides` via `AgentCliConfigArgs`.

## JSONL event mapping

Parser: `src/API/Infrastructure/Agents/Codex/CodexNdjsonParser.cs`

| Codex `type` | Orchi `AgentEvent` |
|--------------|--------------------|
| `thread.started` | `AgentSessionStartedEvent` (`thread_id`) |
| `item.completed` + `agent_message` / `assistant_message` | `AgentTextDeltaEvent` |
| `item.started` + tool-like items | `AgentToolEvent` |
| `turn.completed` | `AgentCompletedEvent` |
| `turn.failed` / `error` | `AgentErrorEvent` |

## Models and context sizes

Codex has no Orchi CLI model sync — add model slugs under **Settings → Agents**.

Context sizes map to Codex `-c model_context_window={tokens}`. Use presets that match the
[Codex advanced config](https://developers.openai.com/codex/config-advanced) catalog defaults:

| Preset id | Tokens | Notes |
|-----------|--------|-------|
| `compact` | 128000 | Example value from Codex config docs |
| `default` | 272000 | Common Codex model catalog default |
| `large` | 400000 | Larger window (clamped per-model by Codex `max_context_window`) |

Settings → Agents shows one-click preset buttons for Codex and a Docs link to the official page.
Mode defaults pick agent + model + context per Orchi mode (e.g. orchestration → Codex / default).

## CLI options catalogs (reasoning + approval)

Orchi keeps **user-populated catalogs** of Codex config knobs per agent. These are not auto-synced from the CLI — you add the values you care about under **Settings → Agents**.

| Kind | Codex `-c` key | Typical Codex presets |
|------|----------------|------------------------|
| `model_reasoning_effort` | `model_reasoning_effort` | `minimal`, `low`, `medium`, `high`, `xhigh` |
| `approval_policy` | `approval_policy` | `untrusted`, `on-request`, `never` |

API surface: `GET/POST /agents/{agentId}/cli-options/{kind}`, plus enable/disable and delete by option id.

When a chat (or mode default) selects an option id, `AgentSessionManager` hydrates `CliConfigOverrides` with the catalog’s `CliValue`. Mode defaults and the chat composer expose the same catalogs as dropdowns.

Official reference: [Codex advanced configuration](https://developers.openai.com/codex/config-advanced) (also mirrored at learn.chatgpt.com docs for `config.toml`).

## Further reading

- [Codex CLI](https://developers.openai.com/codex/cli) — official install and usage
- [Codex advanced configuration](https://developers.openai.com/codex/config-advanced) (`model_context_window`, `model_reasoning_effort`, `approval_policy`)
- [openai/codex#16337](https://github.com/openai/codex/issues/16337) — upstream context on Windows `.cmd` shim resolution (not in official docs)
- [Adapters](adapters.md)
- [Agent overview](README.md)
