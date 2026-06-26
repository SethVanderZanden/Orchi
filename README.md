# Orchi

Orchi is an open-source AI engineering orchestrator that coordinates multiple coding agents, Git worktrees, and review workflows into a repeatable software delivery pipeline. Bring your own AI subscriptions; Orchi manages the engineering process.

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
└── architecture/      # VSA, screaming architecture, CQRS pipeline guides
```

## Architecture

The API uses **Vertical Slice Architecture** with a custom CQRS pipeline (no MediatR), minimal APIs, and OpenAPI + Scalar for interactive documentation. See [docs/architecture/README.md](docs/architecture/README.md) for the full guide.

Run API tests:

```bash
npm run test:api
```

## Development

Install root dependencies (dev orchestration):

```bash
npm install
```

Install desktop dependencies:

```bash
npm install --prefix src/desktop
```

Run the full stack (API + Electron desktop):

```bash
npm run dev
```

Or run each part separately:

```bash
npm run dev:api       # API at http://localhost:5265
npm run dev:desktop   # Electron app with Vite HMR
```

### Verify connectivity

1. API: open [http://localhost:5265/scalar/v1](http://localhost:5265/scalar/v1) — Scalar API docs (default launch target).
2. API data: [http://localhost:5265/WeatherForecast](http://localhost:5265/WeatherForecast) — sample endpoint returning JSON forecast data.
3. Desktop: the Electron window should display the same forecast in a table.

### Desktop stack

The Electron app uses **TanStack Router** (file-based routes in `src/desktop/src/renderer/src/routes/`) and **TanStack Query** (API state in `src/desktop/src/renderer/src/lib/`).

### shadcn/ui components

Run shadcn commands from the **desktop project root** — the folder that contains `components.json`:

```bash
cd src/desktop
npx shadcn@latest add button
```

Components are written to `src/desktop/src/renderer/src/components/ui/`. The `@/` alias maps to `src/renderer/src/`.

## Database migrations

From the repo root:

```bash
dotnet ef migrations add <MigrationName> --project src/API
dotnet ef database update --project src/API
```
