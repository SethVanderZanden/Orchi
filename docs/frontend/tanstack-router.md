# TanStack Router

## Dummy section (start here)

Think of your app as a **building with fixed hallways and swap-out rooms**.

- The **hallways** (header tabs, shell) stay put — you walk the same path no matter which room you enter.
- Each **room** is a page — chat, settings, etc.
- **`<Outlet />`** is the **doorway** where the current room connects to the hallway. The layout says: "everything around this doorway stays; only the room behind the door changes."

When you open a chat tab, you are not rebuilding the whole building. You are walking to a different room. The header shell never unmounts.

**Blazor version:** this is like a `LayoutComponent` with `@Body` — the layout renders once; `@Body` is where the active page appears.

**Next.js version:** this is like `layout.tsx` wrapping `{children}` — the layout persists; the child page swaps.

**Orchi version:**

| Building part | Orchi code |
|---------------|------------|
| Whole building bootstrap | `main.tsx` → `RouterProvider` |
| Global providers (API cache) | `routes/__root.tsx` |
| Fixed hallway (header layout) | `routes/_app.tsx` → `AppLayout` |
| Doorway where page appears | `<Outlet />` in `app-layout.tsx` |
| Individual room | `routes/_app/chat.$chatId.tsx`, `settings.tsx`, etc. |

Everything below explains how that works in code.

---

TanStack Router handles **client-side routing** in the Orchi desktop app: which page is visible, the URL path, and navigation between pages — with TypeScript-safe links and params.

**Official docs:** [tanstack.com/router](https://tanstack.com/router)

## What is it?

TanStack Router is a **router for React SPAs** (single-page apps). It:

- Maps **URL paths** to **page components**
- Supports **nested layouts** (shared shell + changing content)
- Provides **`<Link>`**, **`useNavigate`**, **`useParams`** for navigation
- Generates a **typed route tree** from files in `routes/`

Orchi uses **file-based routing**: each file in `src/desktop/src/renderer/src/routes/` becomes a route. The plugin writes `routeTree.gen.ts` — do not edit that file by hand.

## Why use it?

| Problem | How the router helps |
|---------|----------------------|
| Share shell across pages | Layout route + `<Outlet />` — layout stays mounted |
| Bookmark / refresh a specific chat | URL `/chat/abc-123` maps to a real route |
| Type-safe navigation | `Link to="/chat/$chatId" params={{ chatId }}` — typos caught at compile time |
| Code-split pages | Router plugin can lazy-load route modules |

Without a router, you would manually swap components with `useState` and lose URLs, back-button behaviour, and clear structure as the app grows.

## How Orchi is wired

### Bootstrap

```tsx
// main.tsx
const router = createRouter({ routeTree })
createRoot(...).render(<RouterProvider router={router} />)
```

`routeTree` comes from auto-generated `routeTree.gen.ts` based on your `routes/` folder.

### Route tree (simplified)

```
__root.tsx                    ← QueryClientProvider, devtools
  └── _app.tsx                ← pathless layout: ChatProvider + header tabs
        ├── index.tsx         → URL: /
        ├── chat.$chatId.tsx  → URL: /chat/:chatId
        └── settings.tsx      → URL: /settings
```

`_app` is a **pathless layout** — the `_` prefix means it does not add a URL segment. It only wraps child routes with shared UI.

### What is `<Outlet />`?

**`<Outlet />` is a placeholder** where the **active child route** renders.

Parent layout:

```tsx
// components/layout/app-layout.tsx
<AppHeader />
<div className="flex-1">
  <Outlet />   {/* ← current page renders HERE */}
</div>
```

When you navigate to `/settings`, the router renders `SettingsPage` inside that `<Outlet />`. The header and providers above it **do not re-mount**.

**Analogies:**

| Framework | Equivalent |
|-----------|------------|
| **Blazor** | `@Body` inside `MainLayout.razor` |
| **Next.js App Router** | `{children}` in `layout.tsx` |
| **TanStack Router** | `<Outlet />` in a layout route |

There can be multiple nested outlets (layout inside layout). Orchi uses two levels:

1. `__root.tsx` → `<Outlet />` for the whole app under providers
2. `AppLayout` → `<Outlet />` for the main content under the header tabs

### How navigation works

Navigation = **change the URL** → router picks the matching route → renders it in the nearest `<Outlet />`.

**Declarative (click a link):**

```tsx
import { Link } from '@tanstack/react-router'

<Link to="/chat/$chatId" params={{ chatId: chat.id }}>
  {chat.title}
</Link>
```

**Programmatic (after an action):**

```tsx
import { useNavigate } from '@tanstack/react-router'

const navigate = useNavigate()
navigate({ to: '/chat/$chatId', params: { chatId: newChat.id } })
```

**Redirect (replace bad URL):**

```tsx
import { Navigate } from '@tanstack/react-router'

if (!chat) {
  return <Navigate to="/" replace />
}
```

The browser/Electron history stack updates, so back/forward works like a normal app.

### Route params

Dynamic segments use `$` in the filename:

| File | URL | Access params |
|------|-----|---------------|
| `chat.$chatId.tsx` | `/chat/abc-123` | `Route.useParams()` → `{ chatId: 'abc-123' }` |

```tsx
// routes/_app/chat.$chatId.tsx
function ChatPage() {
  const { chatId } = Route.useParams()
  const chat = getChat(chatId)
  ...
}
```

### Knowing which route is active

For active-tab highlighting, Orchi uses `useMatch`:

```tsx
const chatMatch = useMatch({
  from: '/_app/chat/$chatId',
  shouldThrow: false
})
const activeChatId = chatMatch?.params.chatId ?? null
```

This reads the current URL without passing props through every component.

## File-based routing cheat sheet

| File | Route id | Browser URL |
|------|----------|-------------|
| `routes/__root.tsx` | `__root__` | (wrapper only) |
| `routes/_app.tsx` | `/_app` | (pathless layout) |
| `routes/_app/index.tsx` | `/_app/` | `/` |
| `routes/_app/settings.tsx` | `/_app/settings` | `/settings` |
| `routes/_app/chat.$chatId.tsx` | `/_app/chat/$chatId` | `/chat/$chatId` |

### Adding a new page

1. Create `routes/_app/your-page.tsx`
2. Run dev/build (or `npx @tanstack/router-cli generate` with `tsr.config.json`)
3. Navigate with `<Link to="/your-page">` — path matches filename

Example:

```tsx
// routes/_app/agents.tsx
import { createFileRoute } from '@tanstack/react-router'
import { AppPageHeader } from '@/components/layout/app-page-header'

export const Route = createFileRoute('/_app/agents')({
  component: AgentsPage
})

function AgentsPage() {
  return (
    <div className="flex h-full flex-col">
      <AppPageHeader title="Agents" />
      <main className="p-6">...</main>
    </div>
  )
}
```

The header layout applies automatically because the file lives under `_app`.

## Layout vs page state

| Put it in… | When… |
|------------|-------|
| **Layout** (`AppLayout`, `_app.tsx`) | UI shared across routes — header tabs, providers that must survive navigation |
| **Page route** (`chat.$chatId.tsx`) | Content specific to one URL |
| **React context** (`ChatProvider`) | Data shared across routes but not in the URL — chat list, search filter |

Chat list lives in `ChatProvider` so it persists when you visit Settings. Active chat **id** comes from the **URL** so links are shareable and open tabs stay in sync.

## Regenerating routes

`routeTree.gen.ts` is auto-generated. Config: `src/desktop/tsr.config.json`.

```bash
cd src/desktop
npx @tanstack/router-cli generate
```

Vite dev/build also regenerates it via the TanStack Router plugin.

## FAQ

### Is this server-side routing like Next.js?

**No.** Orchi desktop is an Electron + Vite **client-side SPA**. All routing happens in the renderer process after the app loads. There is no server rendering pages per request (unlike Next.js App Router on the server).

The **layout + children pattern** feels similar to Next.js, but URLs are handled in-browser, not by a Next server.

### How is this different from Blazor routing?

| Blazor | TanStack Router |
|--------|-----------------|
| `@page "/path"` on a component | File `routes/.../path.tsx` |
| `@Body` in layout | `<Outlet />` |
| `<NavLink href="...">` | `<Link to="...">` |
| `@inject NavigationManager` | `useNavigate()` |
| Route params `[Parameter]` | `$param` in filename |

Both give you layouts that persist and a slot for page content. TanStack Router is React-specific and file-based in Orchi.

### What happens when I navigate between pages?

1. URL changes (e.g. `/chat/1` → `/settings`)
2. Router matches the new route definition
3. Old **page** component unmounts; **layout** components stay mounted
4. New page renders inside `<Outlet />`
5. React re-renders only what changed

Open tabs are session-only (`ChatTabsProvider`); the active chat URL stays in sync with the selected tab.

### Does "paging" mean pagination?

In this doc, **pages** means **screens/routes** (chat page, settings page) — not table pagination.

TanStack Router does not paginate API data. For that, see [TanStack Query](tanstack-query.md) or UI components like shadcn `Pagination`.

### Why both `__root` and `_app` layouts?

- **`__root`** — app-wide providers (QueryClient, devtools). No visual chrome.
- **`_app`** — Orchi shell (sidebar + main area). All product pages nest here.

Splitting keeps global infrastructure separate from product layout.

## Further reading

- [Frontend overview](README.md)
- [TanStack Query](tanstack-query.md) — API data (often used inside route pages)
- [TanStack Router docs](https://tanstack.com/router/latest/docs/framework/react/overview)
