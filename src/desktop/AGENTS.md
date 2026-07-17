# AGENTS

Project-specific guidance for AI coding agents working on the Orchi desktop app.

## Stack

| Layer        | Technology                                                  |
| ------------ | ----------------------------------------------------------- |
| Shell        | Electron + electron-vite                                    |
| UI           | React 19, TypeScript                                        |
| Routing      | TanStack Router (file routes in `src/renderer/src/routes/`) |
| Server state | TanStack Query                                              |
| Components   | shadcn/ui (Radix + Tailwind) — `components/ui/`             |
| Icons        | lucide-react                                                |
| Styling      | Tailwind CSS 4, CSS variables in `assets/main.css`          |

## Before writing UI

1. Check existing components in `components/ui/` and feature folders
2. Use shadcn CLI from `src/desktop/`: `npx shadcn@latest add <component>`
3. Follow patterns in `docs/frontend/coding-standards.md`
4. Do NOT use `@astryxdesign/*` — migrated to shadcn (see `plans/ui-migration/`)

## Directory conventions

| Path                                     | Purpose                              |
| ---------------------------------------- | ------------------------------------ |
| `src/renderer/src/routes/`               | TanStack file routes — keep thin     |
| `src/renderer/src/providers/`            | React context providers only         |
| `src/renderer/src/hooks/`                | Custom hooks (`useX`) — no providers |
| `src/renderer/src/components/ui/`        | shadcn primitives                    |
| `src/renderer/src/components/{feature}/` | Feature UI                           |
| `src/renderer/src/lib/{domain}/`         | API clients, types, pure logic       |

## Query keys

All React Query keys live in `lib/query-keys.ts` — never inline arrays in components.

## API calls

- Use `getApiBaseUrl()` from `lib/api.ts`
- Use `readErrorMessage()` from `lib/http/read-error-message.ts`
- See `docs/frontend/api-conventions.md`

## Commands

```bash
cd src/desktop
npm run dev          # electron-vite dev
npm run typecheck    # TypeScript
npm run lint         # ESLint
npm run test         # Vitest
npm run format       # Prettier
```

## Related docs

- `docs/frontend/README.md`
- `docs/frontend/plans/README.md` — improvement plans
