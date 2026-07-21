# AGENTS

Root guidance for AI coding agents working on Orchi. See `README.md` for the full developer guide and `src/desktop/AGENTS.md` for desktop-specific conventions.

## Cursor Cloud specific instructions

Orchi has two runnable pieces plus a test project:

- **API** (`src/API`) — .NET 10 Web API (SQLite, auto-migrated on startup). Runs on port `5265`; docs at `/scalar/v1`, health at `/health`.
- **Desktop** (`src/desktop`) — Electron + React + Vite chat shell that proxies to the API.
- **Tests** (`tests/Orchi.Api.Tests`) — xUnit.

Standard lint/test/build/run commands are already documented in `README.md` and `src/desktop/AGENTS.md`; use those. The notes below are the non-obvious, environment-specific caveats.

### Toolchain (already provisioned in the VM snapshot)

- **.NET 10 SDK** lives in `~/.dotnet` and is symlinked at `/usr/local/bin/dotnet` (so `dotnet` works in any shell). `~/.bashrc` also exports `DOTNET_ROOT` and adds `~/.dotnet` + `~/.dotnet/tools` to `PATH`.
- **`dotnet-ef`** global tool is installed (for `dotnet ef migrations` / `database update`). Never handwrite EF migrations unless required and human-approved — see `.cursor/skills/ef-migrations/SKILL.md`.
- **Node 22** + npm are preinstalled. Dependencies are refreshed on startup by the update script (`npm install` at root and in `src/desktop`, plus `dotnet restore Orchi.slnx`).

### Known blocker: the .NET side does not build (pre-existing repo defect)

The API and test project currently fail to compile because the entire production source folder `src/API/Infrastructure/Agents/Plans/Artifacts/` is **missing from git** — it was never committed. It defines types referenced across the API and by committed tests (`OrchiArtifactFileStore`, `IOrchiArtifactWriterFactory`/`OrchiArtifactWriterFactory`, `IOrchiArtifactWriterStrategy` + `ImplementationPlanWriterStrategy`/`ReviewBriefWriterStrategy`, `IOrchiArtifactTaskFactory`/`OrchiArtifactTaskFactory`, `IOrchiArtifactTaskStrategy` + `ImplementationPlanTaskStrategy`/`ReviewPlanTaskStrategy`, and the `OrchiArtifactKind` enum).

Root cause: the root `.gitignore` rule `artifacts/` matches the `Artifacts/` source folder on case-insensitive filesystems (Windows/macOS), so `git add` silently skipped it. Until the author commits those files (and narrows the ignore rule to `/artifacts/` so it only targets .NET build output at the repo root), `dotnet build`, `dotnet test tests/Orchi.Api.Tests`, and `dotnet run --project src/API` all fail with `CS0234`/`CS0246` for the `Artifacts` namespace. This is a code defect, not an environment problem — do not try to "fix" it via setup.

### Running the desktop app in this headless VM

- The root `npm run dev` / `setup:runtime` / `start:runtime` scripts are **PowerShell + Windows-only** and do not run on Linux. Run the pieces directly instead: `npm run dev --prefix src/desktop` (Electron) and `dotnet run --project src/API` (API, once it builds).
- Launch Electron with a display and the sandbox disabled: `DISPLAY=:1 ELECTRON_DISABLE_SANDBOX=1 npm run dev --prefix src/desktop`. The `dbus`/GPU/`viz` errors it prints are benign in this container.
- With the API down, the desktop shell still renders but shows `API error: 500` / proxy `ECONNREFUSED` for `/chats` and `/projects`. Meaningful end-to-end chat flows require the API (blocked above) **and** an installed, authenticated Cursor CLI (`agent`) on the same machine.

### Test caveats

- Desktop `npm run lint` currently reports pre-existing ESLint errors/warnings in committed code (the tooling itself runs fine).
