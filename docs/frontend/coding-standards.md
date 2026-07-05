# Coding standards (desktop frontend)

## Dummy section (start here)

Building the Orchi desktop UI is like organizing a **workshop**: every tool has a pegboard spot, and you grab the right one without hunting through junk drawers.

- **Pegboard** = folder layout (`providers/`, `hooks/`, `lib/chat/`, …)
- **Power tools** = TanStack Query and shadcn components — shared, not reinvented per screen
- **Hand tools** = local `useState` for one-off UI (draft text, panel open/closed)

If you bolt a provider into the hooks drawer or fetch API data inside a button component, the next person (human or agent) will put things back in the wrong place.

| Workshop spot | Orchi folder |
|---------------|--------------|
| Back-office systems | `providers/` — React context only |
| Tools employees carry | `hooks/` — `useX` hooks, no provider components |
| Off-the-shelf parts | `components/ui/` — shadcn primitives |
| Custom assemblies | `components/{feature}/` — chat, project, orchestration UI |
| Spec sheets & suppliers | `lib/{domain}/` — API clients, types, pure logic |

**Aha:** File location is documentation — put code where the next developer expects it.

Everything below is the same idea with concrete rules, limits, and checklists.

---

## Stack (quick reference)

| Layer | Technology |
|-------|------------|
| Shell | Electron + electron-vite |
| UI | React 19, TypeScript |
| Routing | TanStack Router — file routes in `routes/` |
| Server state | TanStack Query |
| Components | shadcn/ui (Radix + Tailwind) — `components/ui/` |
| Icons | lucide-react |
| Styling | Tailwind CSS 4, CSS variables in `assets/main.css` |

See [AGENTS.md](../../src/desktop/AGENTS.md) for agent-oriented commands and paths.

## File size limits

| Kind | Target | Notes |
|------|--------|-------|
| Component / hook | ≤ **250 lines** | Split when a file mixes fetch + UI + helpers |
| Provider | ≤ **150 lines** | Compose smaller hooks (see `ChatProvider` + `hooks/chat/*`) |
| Pure lib module | No hard cap | Prefer focused files; add `*.test.ts` for logic |

When a provider grows, extract hooks into `hooks/{feature}/` and keep the provider as a thin composer.

## Provider vs hook rules

| Directory | Contains | Does **not** contain |
|-----------|----------|----------------------|
| `providers/` | React context providers (`XProvider`, `createContext`) | Business logic blocks >50 lines — extract to hooks |
| `hooks/` | Custom hooks (`useX`), context consumers | Provider components or JSX trees |
| `components/ui/` | shadcn primitives | Domain logic or API calls |
| `components/{feature}/` | Feature UI | Direct `fetch` — use `lib/` + Query |
| `lib/{domain}/` | API, types, pure functions | React components |

**Pattern:** Provider creates context and composes hooks; hook file exports `useX()` that reads context or encapsulates behavior.

Example — chat after Plan 04 decomposition:

```
ChatProvider (providers/chat-provider.tsx)
  ├── useChatList
  ├── useChatCache
  ├── useChatStream      ← SSE + markers live here
  ├── useChatMutations
  └── useChatOrchestration
```

See [Plan 07 — Provider organization](plans/07-provider-component-organization.md).

## Import rules

- Always use the `@/` path alias — **no** relative `../` imports across folders
- Group imports: external packages → `@/components` → `@/hooks` → `@/lib` → relative (same folder only)

```typescript
import { useQuery } from '@tanstack/react-query'

import { Button } from '@/components/ui/button'
import { useChat } from '@/providers/chat-provider'
import { chatKeys } from '@/lib/query-keys'
```

## Component naming

- **Files:** kebab-case matching export — `chat-mode-dropdown.tsx` → `ChatModeDropdown`
- **Components:** PascalCase
- **Feature prefix** when helpful: `Chat*`, `Plan*`, `Project*` (e.g. `ChatComposer`, `PlanCards`, `ProjectNavigator`)
- **Hooks:** camelCase with `use` prefix — `use-plan-review.ts` → `usePlanReview`

## State ownership

| Data kind | Where it lives | Example |
|-----------|----------------|---------|
| Server / API data | TanStack Query + `lib/query-keys.ts` | Chat list, agent modes, projects |
| Cross-route UI | Context provider | `ChatProvider` streaming flags, `ProjectLayoutProvider` sidebar |
| Ephemeral UI | Local `useState` | Draft message, dialog open, search filter |
| Complex local UI | `useReducer` | Review panel tabs — see `hooks/use-plan-review.ts` |

**Rules:**

- Do not duplicate server data in context — read from Query cache via `useQuery` or `queryClient.getQueryData`
- Do not put URL state in context — use TanStack Router params/search
- Streaming markers and in-flight send state stay in `useChatStream`, not in API modules

See [TanStack Query](tanstack-query.md) for cache vs context boundaries.

## Styling

- Use the `cn()` helper from `@/lib/utils` to merge Tailwind classes
- Prefer shadcn design tokens (`bg-background`, `text-muted-foreground`, `border-border`) — **no raw hex colors**
- Add new shadcn components from `src/desktop/`: `npx shadcn@latest add <component>`
- Do **not** use `@astryxdesign/*` — migrated to shadcn (see archived [ui-migration tracker](../../src/desktop/plans/ui-migration/README.md))

## Testing

- **Pure logic** in `lib/` must have colocated `*.test.ts` (parsers, merge helpers, query-key usage)
- **Components:** add tests when behavior is easy to regress (reducers, formatters, critical UI states)
- Run from `src/desktop/`: `npm run test`, `npm run typecheck`, `npm run lint`

Vitest config: `src/desktop/vitest.config.ts`.

## Vocabulary (Project vs workspace)

| Term | Meaning in Orchi |
|------|------------------|
| **Project** | Top-level sidebar group — repo/folder on disk (`ProjectProvider`, `projectKeys`) |
| **Workspace** | Sub-path within a project — API still exposes `/workspaces` routes |
| **Chat** | Belongs to a project (+ optional workspace id from API) |

UI code should say **project** for sidebar grouping. Reserve **workspace** for API types and sub-path features only.

See [Plan 05 — Projects / workspaces naming](plans/05-projects-workspaces-naming.md).

## API and query keys

- HTTP modules live in `lib/{domain}/api.ts` — see [API conventions](api-conventions.md)
- All TanStack Query keys in `lib/query-keys.ts` — never inline `queryKey: ['…']` in components
- Use `getApiBaseUrl()` and `readErrorMessage()` for every fetch

## PR checklist

Before opening a PR that touches the desktop renderer:

- [ ] `cd src/desktop && npm run typecheck`
- [ ] `npm run lint`
- [ ] `npm run test`
- [ ] Manual smoke: navigate, send a message or open settings if relevant
- [ ] New API surface documented in [api-conventions.md](api-conventions.md) if added
- [ ] No new `@astryxdesign` or inline query keys

## Related docs

- [Frontend overview](README.md)
- [API conventions](api-conventions.md)
- [TanStack Query](tanstack-query.md)
- [Chat streaming](chat-streaming.md)
- [Improvement plans](plans/README.md)
