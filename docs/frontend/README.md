# Frontend (Desktop)

Guides for the Orchi Electron desktop app — React, TanStack Router, TanStack Query, and shadcn/ui.

**Code lives in:** `src/desktop/src/renderer/src/`

## Guides

| Guide | What you'll learn |
|-------|-------------------|
| [TanStack Router](tanstack-router.md) | Routes, layouts, `<Outlet />`, navigation — start with the [Dummy section](tanstack-router.md#dummy-section-start-here) |
| [TanStack Query](tanstack-query.md) | Fetching API data, caching, loading/error states — start with the [Dummy section](tanstack-query.md#dummy-section-start-here) |
| [Chat streaming](chat-streaming.md) | SSE message flow, markers, ChatProvider — start with the [Dummy section](chat-streaming.md#dummy-section-start-here) |

## Coming from Blazor or Next.js?

Both guides include comparison tables mapping familiar concepts to Orchi's setup. Short version:

| You know… | Closest Orchi equivalent |
|-----------|-------------------------|
| Blazor `@Body` in a layout | TanStack Router `<Outlet />` |
| Blazor `@page "/route"` | File in `routes/` → URL path |
| Next.js `app/layout.tsx` + `{children}` | `routes/_app.tsx` + `<Outlet />` |
| Next.js `app/page.tsx` | `routes/_app/chat.$chatId.tsx` (example page) |
| Calling an API in `OnInitializedAsync` | TanStack Query `useQuery` |
| Manual `HttpClient` + field to store result | Query cache handles fetch + storage for you |

## Stack at a glance

```
main.tsx
  └── RouterProvider          ← TanStack Router (which page?)
        └── __root.tsx
              └── QueryClientProvider   ← TanStack Query (API data)
                    └── _app layout     ← sidebar + Outlet
                          └── page routes (chat, settings, …)
```

## Related docs

- [Documentation convention](../patterns/DOCUMENTATION.md) — Dummy section template
- [Architecture overview](../architecture/README.md)
