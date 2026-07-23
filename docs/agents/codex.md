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
| Recipe card | `--model` + `-c model_context_window=N` + `-c model_reasoning_effort=…` (no `approval_policy`; exec is headless) |
| Ticket number | `thread_id` stored as `ExternalSessionId` |
| Plated courses | `AgentTextDeltaEvent`, `AgentToolEvent`, `AgentCompletedEvent` |

**Aha:** one chat picks the chef and the recipe knobs; Orchi turns those into Codex `-c` overrides without you typing the CLI by hand. In Codex you see “5.6 Terra Medium”; in Orchi that is **model** `gpt-5.6-terra` + **reasoning** `medium`.

Everything below is the same idea with adapters, catalogs, and JSONL.

---

## Prerequisites

1. Install the [OpenAI Codex CLI](https://developers.openai.com/codex/cli) (`codex` on PATH)
   - **Windows (recommended):** run the [PowerShell installer](https://developers.openai.com/codex/cli) from the official docs, or `npm install -g @openai/codex`
   - **macOS / Linux:** `npm install -g @openai/codex` or the curl installer on the same page
2. Authenticate per Codex docs
3. Run the Orchi API on the same machine

### API process PATH (Windows)

Your terminal may find `codex` while the Orchi API cannot — or the API may find a file that **cannot be started**. This is common on Windows after installing Codex via npm **and/or** the PowerShell installer.

npm global installs place several launchers in the same directory (for example `C:\Program Files\nodejs\` or `%APPDATA%\npm\`):

| File | What it is | Works from a shell? | Works with `Process.Start`? |
|------|------------|---------------------|-----------------------------|
| `codex` | Bash shim (no extension) | Often yes (Git Bash / shell PATH) | **No** — not a Windows executable |
| `codex.cmd` | Windows batch shim | Yes | Yes (via `cmd.exe /c`) |
| `codex.ps1` | PowerShell shim | Yes (PowerShell) | No (unless invoked through PowerShell) |
| `codex.exe` | Native CLI (PowerShell installer or npm platform package) | Yes | Yes |

Codex’s official docs cover [installation](https://developers.openai.com/codex/cli) but do **not** document this shim layout. The behavior is discussed in upstream issues such as [openai/codex#16337](https://github.com/openai/codex/issues/16337) (Windows `spawn` / `.cmd` shim resolution).

Orchi resolves the executable before spawning:

1. Absolute path from `Agents:Codex:Executable` (if set and file exists)
2. **Native `codex.exe`** in known Windows install dirs:
   - `%LOCALAPPDATA%\Programs\OpenAI\Codex\bin` (PowerShell / standalone installer)
   - `%LOCALAPPDATA%\OpenAI\Codex\bin` (Codex app CLI)
   - npm platform package binaries under `node_modules/@openai/codex-win32-*`
3. Search merged user + machine PATH with `PATHEXT` (`.exe`, `.cmd`, …), **skipping extensionless shims on Windows**
4. Windows fallback: `%LOCALAPPDATA%\…\codex.exe`, then `%APPDATA%\npm\codex.cmd`
5. **Last resort:** `node.exe` + `@openai/codex/bin/codex.js` (npm layout only when no `.exe`/`.cmd` was found)

Orchi prefers **`codex.exe`** and **`codex.cmd`** over launching `node.exe` directly. Preferring the npm node-bundle first could pick a stale `C:\Program Files\nodejs\node.exe` layout even when a working native `codex` (the one your terminal runs) is already installed.

When only a `.cmd` shim is found, Orchi launches it via `cmd.exe /c` so stdout/stderr redirection still works.

**If spawn still fails:**

- Restart the Orchi API after installing Codex
- Confirm `codex --version` works in a **new** terminal, then restart the API host (Visual Studio, `dotnet run`, etc.) so it picks up PATH changes
- Confirm the chat workspace folder exists on disk (invalid `WorkingDirectory` makes `Process.Start` fail)
- Set a full path in `appsettings.json`:

```json
"Agents": {
  "Codex": {
    "Executable": "C:\\Users\\<you>\\AppData\\Local\\Programs\\OpenAI\\Codex\\bin\\codex.exe"
  }
}
```

npm alternative:

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
      "workspace-write"
    ],
    "TimeoutSeconds": 600
  }
}
```

## Spawn command

One process per user turn:

```bash
codex exec --json [--skip-git-repo-check] \
  [--sandbox workspace-write] \
  [--model {slug}] \
  [-c model_context_window={tokens}] \
  [-c model_reasoning_effort={effort}] \
  [resume {threadId}] \
  "{composedPrompt}"
```

Orchi does **not** pass `-c approval_policy=…`. `codex exec` is non-interactive and defaults to `approval_policy=never`; overriding with `on-request` or `untrusted` can stall until the Orchi timeout because exec cannot surface approval prompts ([non-interactive docs](https://developers.openai.com/codex/noninteractive)).

Orchi closes the child process stdin immediately after spawn. Codex treats a piped stdin as extra prompt input and blocks until EOF; API hosts often inherit an open stdin pipe, which otherwise leaves chats stuck on "…" with no JSONL events.

Working directory is the chat workspace path. Model, context, reasoning effort, and approval policy come from the chat (mode defaults or composer overrides). Extra `-c` keys are assembled from `ChatSession.CliConfigOverrides` via `AgentCliConfigArgs`.

## JSONL event mapping

Parser: `src/API/Infrastructure/Agents/Codex/CodexNdjsonParser.cs`

| Codex `type` | Orchi `AgentEvent` |
|--------------|--------------------|
| `thread.started` | `AgentSessionStartedEvent` (`thread_id`) |
| `turn.started` | `AgentToolEvent` (`Working…`) |
| `item.completed` + `agent_message` / `assistant_message` | `AgentTextDeltaEvent` |
| `item.started` + tool-like items | `AgentToolEvent` |
| `turn.completed` | `AgentCompletedEvent` |
| `turn.failed` / fatal errors | `AgentErrorEvent` |
| `error` (transient reconnect) | ignored (wait for `turn.failed`) |

## Models and context sizes

Codex has no Orchi CLI model sync. On API startup (and when you enable Codex), Orchi seeds
GPT-5.6 **Sol / Terra / Luna** with Codex-style labels (`5.6 Terra`, …) plus reasoning
presets including `none` and `max`.

In Codex itself you pick combined names like **“5.6 Terra Medium”** — that is model
`gpt-5.6-terra` plus reasoning effort `medium`. Orchi stores those as two fields and shows
the combined label in mode defaults and the first-run setup wizard.

| Preset | Model id (`--model`) | Typical use |
|--------|----------------------|-------------|
| 5.6 Sol | `gpt-5.6-sol` | Complex / high-value work |
| 5.6 Terra | `gpt-5.6-terra` | Everyday orchestration & review (default) |
| 5.6 Luna | `gpt-5.6-luna` | Fast, clear, high-volume tasks |

Settings → Agents uses an agent settings **strategy**: Cursor gets CLI sync; Codex gets
curated presets + reasoning/approval cards (no empty sync button).

Context sizes map to Codex `-c model_context_window={tokens}`. Use presets that match the
[Codex advanced config](https://developers.openai.com/codex/config-advanced) catalog defaults:

| Preset id | Tokens | Notes |
|-----------|--------|-------|
| `compact` | 128000 | Example value from Codex config docs |
| `default` | 272000 | Common Codex model catalog default |
| `large` | 400000 | Larger window (clamped per-model by Codex `max_context_window`) |

Settings → Agents shows one-click preset buttons for Codex and a Docs link to the official page.
Mode defaults pick agent + model + context + reasoning per Orchi mode (e.g. orchestration → Codex / Terra Medium).

First launch walks you through:

1. Agent selection (Cursor / Codex)
2. Codex approval policy (if Codex is enabled)
3. Mode defaults for **Orchestrator**, **Implementation/default**, and **Review**

Orchi seeds Codex reasoning and model presets on API startup and during first-time agent setup.

The approval policy in Settings applies to interactive Codex usage documentation only; Orchi spawns `codex exec`, which always runs with Codex's headless default (`approval_policy=never`).

| Kind | Codex `-c` key | Typical Codex presets |
|------|----------------|------------------------|
| `model_reasoning_effort` | `model_reasoning_effort` | `none`, `minimal`, `low`, `medium`, `high`, `xhigh`, `max` |
| `approval_policy` | `approval_policy` | `untrusted`, `on-request`, `never` |

API surface: `GET/POST /agents/{agentId}/cli-options/{kind}`, plus enable/disable and delete by option id.
`POST /agents/{agentId}/models` accepts optional `label` for friendly display names.

When a chat (or mode default) selects an option id, `AgentSessionManager` hydrates `CliConfigOverrides` with the catalog’s `CliValue`. Mode defaults and the chat composer expose the same catalogs as dropdowns.

Official reference: [Codex models](https://developers.openai.com/codex/models) and [Codex advanced configuration](https://developers.openai.com/codex/config-advanced).

## Further reading

- [Codex CLI](https://developers.openai.com/codex/cli) — official install and usage
- [Codex advanced configuration](https://developers.openai.com/codex/config-advanced) (`model_context_window`, `model_reasoning_effort`, `approval_policy`)
- [openai/codex#16337](https://github.com/openai/codex/issues/16337) — upstream context on Windows `.cmd` shim resolution (not in official docs)
- [Adapters](adapters.md)
- [Agent overview](README.md)
