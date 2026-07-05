# TanStack Query

## Dummy section (start here)

Imagine you ask a colleague for the weather forecast. You don't call them **every time** you glance at the window — you remember what they said until it feels stale, then you ask again.

**TanStack Query** is that memory for API calls:

- **First visit** — fetch data, show loading
- **Come back soon** — use cached answer (fast, no spinner)
- **Data gets old** — refetch in the background
- **Something failed** — retry or show error

It separates **"get data from the server"** from **"when should I ask again?"**

**Blazor version:** there is no built-in equivalent. Closest mental model: inject `HttpClient`, call in `OnInitializedAsync`, store in a field — but you'd manually handle caching, refetch, and loading flags. TanStack Query automates that.

**Next.js version:** Server Components fetch on the server per request. TanStack Query is for **client-side** fetching after the app is running — closer to using `fetch` in a Client Component plus a smart cache (or libraries like SWR).

**Orchi version:**

| Idea | Orchi code |
|------|------------|
| Shared cache instance | `queryClient` in `lib/query-client.ts` |
| Provide cache to React tree | `QueryClientProvider` in `routes/__root.tsx` |
| "Remember this request" key | `queryKeys` in `lib/query-keys.ts` (`chatKeys`, `projectKeys`, `agentKeys`) |
| Fetch function | `listAgentModes()` in `lib/chat/api.ts`, chat fns in same module |
| Use in a component | `useQuery({ queryKey, queryFn })` |

Chat uses **TanStack Query** for list/detail (`chatKeys`) plus **React context** (`ChatProvider`) for streaming state, markers, and send-message orchestration. See [chat-streaming.md](chat-streaming.md).

Everything below is the full picture.

---

TanStack Query (formerly React Query) manages **server state** in React: fetching, caching, syncing, and updating data from APIs.

**Official docs:** [tanstack.com/query](https://tanstack.com/query)

## What is it?

**Server state** = data that lives on a server (or API) and can change without you knowing. Examples: chat list, agent modes, project list.

TanStack Query provides:

- **`useQuery`** — read data (GET-style)
- **`useMutation`** — write data (POST/PUT/DELETE) and invalidate cache
- **A global cache** — keyed by `queryKey`, shared across components
- **Loading / error / success states** — built in

It does **not** replace your router. Router = which page. Query = what data that page shows.

## Why use it?

Without Query, every component that needs API data tends to duplicate:

```tsx
// Manual approach — easy to get wrong
const [data, setData] = useState(null)
const [loading, setLoading] = useState(true)
const [error, setError] = useState(null)

useEffect(() => {
  fetch('/api/...')
    .then(setData)
    .catch(setError)
    .finally(() => setLoading(false))
}, [])
```

Problems: no shared cache, refetch logic everywhere, race conditions, stale data after mutations.

With Query:

```tsx
const { data, isLoading, isError, error } = useQuery({
  queryKey: agentKeys.modes(),
  queryFn: listAgentModes,
  staleTime: Infinity
})
```

One line gets fetch + cache + loading + error + refetch rules.

## How Orchi is wired

### 1. Query client

```tsx
// lib/query-client.ts
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,  // treat data as fresh for 30 seconds
      retry: 1
    }
  }
})
```

`staleTime: 30_000` means: for 30 seconds after a successful fetch, Query serves cached data without refetching (unless you force it).

### 2. Provider at root

```tsx
// routes/__root.tsx
<QueryClientProvider client={queryClient}>
  <Outlet />
</QueryClientProvider>
```

Every route and component under `__root` can call `useQuery` / `useMutation`.

### 3. Query keys

Keys uniquely identify cached data:

```tsx
// lib/query-keys.ts — see docs/frontend/api-conventions.md
export const chatKeys = {
  all: ['chats'] as const,
  lists: () => [...chatKeys.all, 'list'] as const,
  detail: (chatId: string) => [...chatKeys.all, 'detail', chatId] as const
}

export const agentKeys = {
  all: ['agents'] as const,
  modes: () => [...agentKeys.all, 'modes'] as const,
  modelsForAgent: (agentId: string) => [...agentKeys.all, 'models', agentId] as const,
  models: (agentId: string, includeDisabled = false) =>
    [...agentKeys.all, 'models', agentId, { includeDisabled }] as const,
  modeModelDefaults: (agentId: string) =>
    [...agentKeys.all, 'mode-model-defaults', agentId] as const
}
```

Think of keys like a **file path for cache entries**:

```
cache['chats']['list']           →  ChatThread[] (sidebar)
cache['chats']['detail'][id]     →  ChatThread (messages)
cache['agents']['modes']         →  AgentModeOption[]
cache['agents']['models'][id]    →  AgentModelListResponse
```

Use factories (`chatKeys.detail(id)`) so invalidation stays consistent:

```tsx
queryClient.invalidateQueries({ queryKey: chatKeys.all })  // bust all chat caches
```

### Rule: keys only in `query-keys.ts`

Never define `queryKey: ['agent-modes']` or similar inline in components. Import `chatKeys`, `projectKeys`, or `agentKeys` from `@/lib/query-keys`.

### `agentKeys` — usage and invalidation

**Read** agent config with the matching factory:

```tsx
// Mode dropdown — static list
useQuery({ queryKey: agentKeys.modes(), queryFn: listAgentModes, staleTime: Infinity })

// Settings models card — include disabled models
useQuery({
  queryKey: agentKeys.models(agentId, true),
  queryFn: () => listAgentModels(agentId, true),
  staleTime: 60 * 60 * 1000
})

// Mode default models card
useQuery({
  queryKey: agentKeys.modeModelDefaults(agentId),
  queryFn: () => listAgentModeModelDefaults(agentId)
})
```

**Invalidate** after settings mutations using the **prefix** key so every variant refreshes (enabled-only and include-disabled):

```tsx
// agent-models-card.tsx — after sync, add, remove, or toggle
void queryClient.invalidateQueries({ queryKey: agentKeys.modelsForAgent(agentId) })
```

Use `agentKeys.all` only when every agent-related cache must reset. Prefer `modelsForAgent(agentId)` or `modeModelDefaults(agentId)` for targeted updates.

See [API conventions](api-conventions.md#query-keys-libquery-keysts) for the full key table.

### 4. Fetch function

Pure async function — no React, no hooks:

```tsx
// lib/chat/api.ts
export async function listAgentModes(): Promise<AgentModeOption[]> {
  const response = await fetch(`${getApiBaseUrl()}/agents/modes`)
  if (!response.ok) throw new Error(await readErrorMessage(response))
  return (await response.json()) as AgentModeOption[]
}
```

See [API conventions](api-conventions.md) for base URL, error handling, and mapper patterns.

### 5. Consume in a component

```tsx
const { data, isLoading, isError, error } = useQuery({
  queryKey: agentKeys.modes(),
  queryFn: listAgentModes,
  staleTime: Infinity
})

if (isLoading) return <p>Loading...</p>
if (isError) return <p>{error.message}</p>
return <ModeDropdown options={data} />
```

## Core concepts

### Query lifecycle

```
idle → loading → success
              ↘ error → (retry?) → loading → ...
```

| State | Meaning |
|-------|---------|
| `isLoading` | First fetch, no cached data yet |
| `isFetching` | Any fetch in flight (including background refetch) |
| `isError` | Last attempt failed |
| `data` | Latest successful result |

### Stale vs fresh

- **Fresh** — Query won't refetch on mount/window focus (within `staleTime`)
- **Stale** — Query may refetch when component remounts, window refocuses, or network reconnects

Orchi default: 30s fresh window. Adjust per query or globally in `query-client.ts`.

### Cache invalidation

After a mutation (e.g. send message, save settings), tell Query old data is wrong:

```tsx
const mutation = useMutation({
  mutationFn: postMessage,
  onSuccess: () => {
    queryClient.invalidateQueries({ queryKey: chatKeys.detail(chatId) })
  }
})
```

Invalidation marks queries stale → active ones refetch automatically.

## Query vs Router vs React state

Orchi uses three layers — they solve different problems:

| Layer | Tool | Holds… | Example |
|-------|------|--------|---------|
| **URL / pages** | TanStack Router | Which screen, route params | `/chat/abc-123` |
| **Server data** | TanStack Query | API responses, sync | Chat list, chat detail, agent modes |
| **Client UI state** | `useState` / Context | Local, not in URL or API | Sidebar search text, SSE markers, send state |

**Rule of thumb:**

- In the **URL**? → Router params / navigation
- From an **API**? → TanStack Query
- **UI-only**, shared across routes? → Context (e.g. `ChatProvider` for streaming + markers)
- **UI-only**, one component? → `useState`

Chat list and thread content live in Query (`chatKeys`); streaming markers and in-flight send state stay in `ChatProvider` context.

## Blazor comparison

| Task | Blazor (typical) | TanStack Query |
|------|------------------|----------------|
| Fetch on load | `OnInitializedAsync` + `HttpClient` | `useQuery` |
| Store result | private field `_chats` | Query cache (automatic) |
| Loading flag | manual `bool _loading` | `isLoading` |
| Refresh button | call fetch again manually | `refetch()` or invalidate |
| Share data across components | inject service / cascading value | same `queryKey` → same cache |

Blazor's `HttpClient` is the transport. TanStack Query sits **above** `fetch`/`HttpClient` and manages when to call and where to store results.

## Next.js comparison

| Next.js | TanStack Query |
|---------|----------------|
| Server Component `fetch` (RSC) | Client-side `useQuery` |
| `cache: 'force-cache'` on server | `staleTime` on client |
| Re-fetch on navigation (server) | Re-fetch based on stale rules + `queryKey` |
| `'use client'` required | Runs in client components (all of Orchi renderer) |

Orchi Electron app has no RSC — everything is client-rendered. Query fills the "smart client fetch layer" role.

## Example: adding a new API query

**1. Add a fetch function** — `lib/api.ts`

```tsx
export async function fetchAgents(): Promise<Agent[]> {
  const res = await fetch('/agents')
  if (!res.ok) throw new Error(`API error: ${res.status}`)
  return res.json()
}
```

**2. Add query keys** — `lib/query-keys.ts`

```tsx
export const agentKeys = {
  all: ['agents'] as const,
  list: () => [...agentKeys.all, 'list'] as const
}
```

**3. Use in a route page**

```tsx
function AgentsPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: agentKeys.list(),
    queryFn: fetchAgents
  })
  ...
}
```

**4. (Optional) Mutation + invalidate**

```tsx
const createAgent = useMutation({
  mutationFn: (body: CreateAgentRequest) => fetch('/agents', { method: 'POST', ... }),
  onSuccess: () => queryClient.invalidateQueries({ queryKey: agentKeys.all })
})
```

## DevTools

In development, React Query Devtools mount in `__root.tsx` (bottom-left). Inspect cache entries, query states, and manual refetch/invalidate.

## FAQ

### What's the difference between Query and Router?

| | Router | Query |
|---|--------|-------|
| **Question** | "Which page?" | "What data?" |
| **Driven by** | URL path | API + `queryKey` |
| **Orchi example** | `/chat/$chatId` | `GET /chats`, `GET /agents/modes` |

They work together: a route page calls `useQuery` to load its data.

### How does chat use TanStack Query?

`ChatProvider` composes `useChatList` (sidebar `chatKeys.lists()`), `useChatCache` (detail), and `useChatMutations`. The list query uses `refetchOnMount: 'always'`, retries with backoff, and keeps previous data visible while refetching (`placeholderData`). After streaming, list refetches merge with the existing cache via `mergeChatLists` so optimistic kickoff rows are not dropped. Message **streaming** runs through `useChatStream` (SSE + markers in context) — see [chat-streaming.md](chat-streaming.md).

### Do I need Query for every piece of state?

**No.** Button hover, draft message text, sidebar collapsed — keep those in `useState`. Query shines for **async server data** that multiple components might need and that can go stale.

### What if two components use the same queryKey?

They share **one cache entry**. Both see the same `data`. Only one network request runs (Query deduplicates in-flight fetches).

### How does this relate to the .NET API CQRS "Query"?

Unrelated naming. .NET Orchi **queries** are backend read handlers (`IQueryHandler`). TanStack **Query** is a frontend React library. Different layers, same English word.

## Further reading

- [Frontend overview](README.md)
- [Chat streaming (SSE)](chat-streaming.md)
- [TanStack Router](tanstack-router.md) — layouts, `<Outlet />`, navigation
- [TanStack Query docs](https://tanstack.com/query/latest/docs/framework/react/overview)
