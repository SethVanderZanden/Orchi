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

### `.gitignore` gotcha (previously broke the .NET build — now fixed)

Historical note worth remembering: the root `.gitignore` once used `artifacts/`, which on case-insensitive filesystems (Windows/macOS) also matched the **source** folder `src/API/Infrastructure/Agents/Plans/Artifacts/` and silently kept it out of git, breaking `dotnet build`/`dotnet test`/`dotnet run`. It is now committed and the rule was narrowed to `/artifacts/` (repo-root .NET build output only). Keep that rule root-anchored; do not broaden it back to `artifacts/`.

### Running the desktop app in this headless VM

- The root `npm run dev` / `setup:runtime` / `start:runtime` scripts are **PowerShell + Windows-only** and do not run on Linux. Run the pieces directly instead: `dotnet run --project src/API` (API, port `5265`) and `npm run dev --prefix src/desktop` (Electron). Start the API first so the desktop doesn't show `API error: 500` on load.
- Launch Electron with a display and the sandbox disabled: `DISPLAY=:1 ELECTRON_DISABLE_SANDBOX=1 npm run dev --prefix src/desktop`. The `dbus`/GPU/`viz` errors it prints are benign in this container.
- Windows Chromium lines like `net\disk_cache\blockfile\... Critical error found -8` / `No file for …` are HTTP disk-cache corruption noise. Dev disables that cache; the main process also pins `userData` to an `Orchi` profile (not the electron-vite `desktop` name). If a packaged build still logs them, quit the app and delete the `Cache` / `Code Cache` / `GPUCache` folders under the Orchi userData directory.
- The desktop renders even with the API down, but shows `API error: 500` / proxy `ECONNREFUSED` for `/chats` and `/projects` until the API is up. A new chat is a **draft** in the UI and is not persisted to the DB (`GET /chats` stays empty) until the first message is sent — and sending a message requires an installed, authenticated Cursor CLI (`agent`) on the same machine.

### Test caveats

- `dotnet test tests/Orchi.Api.Tests`: the build succeeds and most tests pass, but a handful fail for reasons unrelated to the environment — several integration tests deserialize enum fields (e.g. chat `status`) with default options that expect numeric enums while the API serializes them as strings, and a couple depend on external `gh`/script behavior. These are pre-existing test-harness issues, not setup problems.
- Desktop `vitest` (`npm run test --prefix src/desktop`): a few tests in `src/main/open-in-editor.test.ts` assert Windows path behavior (`C:\...`) and fail on Linux. Pre-existing OS-specific assumption.
- Desktop `npm run lint` reports pre-existing ESLint errors/warnings in committed code (the tooling itself runs fine).
