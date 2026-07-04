# Orchi

Orchi is a desktop chat shell for coding agents. Bring your own AI subscriptions; Orchi persists chats and routes messages to local agent CLIs (Cursor today).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20.19+ or 22.12+
- npm
- [EF Core CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

## Project structure

```
src/
├── API/       # .NET 10 Web API — vertical slices, CQRS pipeline, Scalar docs
└── desktop/   # Electron + React + Vite + shadcn/ui
tests/
└── Orchi.Api.Tests/   # xUnit handler + integration tests
docs/
├── agents/            # Agent adapters, Cursor CLI, streaming
├── architecture/      # VSA, screaming architecture, CQRS pipeline guides
├── frontend/          # TanStack Router, TanStack Query, chat streaming
└── patterns/          # Software design patterns used in Orchi
```

## Architecture

The API uses **Vertical Slice Architecture** with a custom CQRS pipeline (no MediatR), minimal APIs, and OpenAPI + Scalar for interactive documentation. See [docs/architecture/README.md](docs/architecture/README.md) for the full guide and [docs/patterns/README.md](docs/patterns/README.md) for design patterns.

The agent stack is intentionally minimal: chats persist in SQLite, user messages go straight to `IAgentAdapter`, and multi-turn continuity uses Cursor `--resume`. See [docs/agents/README.md](docs/agents/README.md).

Run API tests:

```bash
npm run test:api
```

## Development

Install dependencies:

```bash
npm install
npm install --prefix src/desktop
```

### Which command do I use?

| Goal | Command | Port |
|------|---------|------|
| **Hack on the repo** (normal daily dev) | `npm run dev` | 5266 if you ran `setup:runtime`, else 5265 |
| **Use the stable published app** | `npm run start:runtime` | 5265 |
| **Build or refresh the published app** | `npm run setup:runtime` | — |

**Typical workflow when you want both:**

```bash
npm run setup:runtime    # once (or when upgrading the stable build)
npm run start:runtime    # your real Orchi — port 5265, own database
npm run dev              # repo changes — port 5266, separate database
```

You do **not** need a special command for parallel dev. After `setup:runtime`, `npm run dev` automatically uses port **5266** so it does not clash with the published app on **5265**.

Run API and desktop separately if needed:

```bash
npm run dev:api       # API only
npm run dev:desktop   # Electron only (uses port from .env.development.local after setup)
```

### Verify connectivity

1. API: [http://localhost:5265/scalar/v1](http://localhost:5265/scalar/v1) (runtime or default dev) or **5266** after `setup:runtime`
2. Desktop: Electron window opens the Orchi chat shell

### Desktop stack

The Electron app uses **TanStack Router** (file-based routes, layouts, navigation) and **TanStack Query** (API caching and fetch state). See:

- [Frontend docs](docs/frontend/README.md)
- [TanStack Router guide](docs/frontend/tanstack-router.md) — layouts, `<Outlet />`, pages
- [TanStack Query guide](docs/frontend/tanstack-query.md) — fetching, caching, server state
- [Chat streaming](docs/frontend/chat-streaming.md) — SSE contract and UI integration

### shadcn/ui components

Run shadcn commands from the **desktop project root** — the folder that contains `components.json`:

```bash
cd src/desktop
npx shadcn@latest add button
```

Components are written to `src/renderer/src/components/ui/`. Imports use the `@/` alias (`@/` → `src/renderer/src/`).

**Important:** `components.json` uses **filesystem paths** (not `@/` aliases) so the CLI writes to the correct folder. If you see a literal `@/` directory appear under `src/desktop/`, the aliases in `components.json` were misconfigured — restore the paths shown in that file.

## Database migrations

From the repo root:

```bash
dotnet ef migrations add <MigrationName> --project src/API
dotnet ef database update --project src/API
```

## Published runtime

Orchi can run as a **single desktop app** (`Orchi.exe`) that starts its own API. Use this for a stable copy while you keep developing in the repo.

```bash
npm run setup:runtime    # build + deploy to %LOCALAPPDATA%\Orchi\app\ (safe to re-run)
npm run start:runtime    # launch stable Orchi on port 5265
npm run dev              # develop in repo on port 5266 (automatic after setup)
```

Runtime chats live under `%LOCALAPPDATA%\Orchi\data\`. Dev chats use `src/API/orchi-dev.db`.

Customize ports in [`scripts/runtime.config.json`](scripts/runtime.config.json).

| Command | Purpose |
|---------|---------|
| `npm run setup:runtime` | Publish API + build desktop + deploy to AppData |
| `npm run start:runtime` | Launch the published app |
| `npm run setup:runtime -- -ResetData` | Wipe runtime chat database |
