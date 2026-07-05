# API conventions

## Dummy section (start here)

Every API call in the desktop app is like sending a **form letter** to the Orchi backend: same envelope rules (base URL, error handling), same filing system for answers (query keys), and a translator at the door (map functions) that turns server JSON into shapes React understands.

If each screen writes its own letter format, you can't find the right cached answer when something changes — and error messages drift apart.

```
Component                    lib/{domain}/
  useQuery ──queryKey──►     query-keys.ts     ← one label per cache slot
       │                     api.ts            ← fetch only
       └──queryFn────────►   types.ts          ← DTO + frontend types
                             mapX() at bottom  ← DTO → UI shape
```

| Analogy | Orchi |
|---------|-------|
| Return address on every letter | `getApiBaseUrl()` from `lib/api.ts` |
| Read rejection slip from server | `readErrorMessage()` from `lib/http/read-error-message.ts` |
| Filing label on cached data | Factory in `lib/query-keys.ts` — never inline arrays in components |
| Translator | Named `mapX()` at bottom of each `api.ts` |

**Aha:** One module layout + shared helpers = predictable fetches, cache invalidation, and error text.

Everything below is the same idea with paths, patterns, and module inventory.

---

## Module layout

```
lib/{domain}/
  api.ts          # fetch functions only
  types.ts        # TypeScript types (+ Zod schemas if adopted)
  *.ts            # pure helpers (parsers, merge, display)
  *.test.ts       # tests for pure helpers
```

Shared HTTP utilities live in `lib/http/` (see [Plan 02 — HTTP & SSE](plans/02-http-sse-infrastructure.md)).

Query key factories live in **`lib/query-keys.ts`** only.

## Required patterns per API function

1. **Base URL** — use `getApiBaseUrl()` from `lib/api.ts`
2. **Errors** — use `readErrorMessage()` from `lib/http/read-error-message.ts` on non-OK responses
3. **Mapping** — map API DTO → frontend type in a named `mapX()` function at the bottom of the file
4. **Failure mode** — throw `Error` with a readable message; do not return `{ ok: false }` unless an established pattern already uses it
5. **Query keys** — import from `lib/query-keys.ts`; never define `queryKey: ['…']` inline in components

### Example shape

```typescript
import { getApiBaseUrl } from '@/lib/api'
import { readErrorMessage } from '@/lib/http/read-error-message'

export async function listThings(): Promise<Thing[]> {
  const response = await fetch(`${getApiBaseUrl()}/things`)

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const raw = (await response.json()) as ThingResponse[]
  return raw.map(mapThing)
}

function mapThing(dto: ThingResponse): Thing {
  return { id: dto.id, name: dto.name }
}
```

## Query keys (`lib/query-keys.ts`)

All TanStack Query keys are centralized:

| Factory | Shape | Typical use |
|---------|-------|-------------|
| `chatKeys.all` | `['chats']` | Invalidate all chat caches |
| `chatKeys.lists()` | `['chats', 'list']` | Sidebar chat list |
| `chatKeys.detail(id)` | `['chats', 'detail', id]` | Single chat thread |
| `projectKeys.lists()` | `['projects', 'list']` | Project picker |
| `projectKeys.detail(id)` | `['projects', 'detail', id]` | Project detail |
| `agentKeys.modes()` | `['agents', 'modes']` | Chat mode dropdown |
| `agentKeys.modelsForAgent(agentId)` | `['agents', 'models', id]` | Invalidate all model queries for an agent |
| `agentKeys.models(agentId, includeDisabled?)` | `['agents', 'models', id, { includeDisabled }]` | Model selectors, settings |
| `agentKeys.modeModelDefaults(agentId)` | `['agents', 'mode-model-defaults', id]` | Mode default models card |

**Invalidation:** use a prefix to bust related keys. Example — after syncing models in settings, invalidate every models query for that agent (both enabled-only and include-disabled variants):

```typescript
void queryClient.invalidateQueries({ queryKey: agentKeys.modelsForAgent(agentId) })
```

See [TanStack Query](tanstack-query.md) for cache lifecycle and [Plan 03](plans/03-query-keys-api-standards.md) for the migration checklist.

## StaleTime conventions

Global default: **30 seconds** in `lib/query-client.ts`.

| Query | Recommended `staleTime` | Notes |
|-------|-------------------------|-------|
| Chat list / detail | default (30s) | Frequent invalidation from streaming + mutations |
| Projects list / detail | default (30s) | |
| Agent modes | `Infinity` | Static config; set on `useQuery` in mode dropdown |
| Agent models | `60_000`–`3_600_000` | Long-lived until settings mutation; invalidate on sync/toggle |

Override on individual `useQuery` calls; do not change the global default unless all queries should behave differently.

## SSE streaming

Long-lived HTTP responses use the shared parser in `lib/http/sse.ts`:

| Function | Purpose |
|----------|---------|
| `parseSseBlock(block)` | Parse one SSE event block (`event:` + `data:` lines) |
| `readSseStream(response, onEvent, signal?)` | Read `fetch` body, invoke callback per event |

Call sites:

| Module | Endpoint | Uses |
|--------|----------|------|
| `lib/chat/api.ts` | `POST /chats/{id}/messages` | `sendMessageStream` → chat token/tool/done events |
| `lib/orchestration/orchestration-events.ts` | `GET /chats/{id}/orchestration/events` | `subscribeOrchestrationEvents` → workflow + child agent events |

SSE streams update TanStack Query cache directly or via context handlers — they do not use query keys for the stream itself. See [chat-streaming.md](chat-streaming.md).

## API module reference

| Module | Base paths | Key functions |
|--------|------------|---------------|
| `lib/chat/api.ts` | `/chats`, `/agents/modes` | `listChats`, `getChat`, `createChat`, `sendMessageStream`, `updateChatMode`, `updateChatModel`, `kickOffPlan`, `kickOffReview`, `closeChat` |
| `lib/projects/api.ts` | `/projects`, `/workspaces` | `listProjects`, `getProject`, `createProject`, `updateProject`, `deleteProject`, `createWorkspace`, `updateWorkspace`, `deleteWorkspace` |
| `lib/chat/agent-models-api.ts` | `/agents/{id}/models` | `listAgentModels`, `syncAgentModels`, `addAgentModel`, `updateAgentModelEnabled`, `removeAgentModel` |
| `lib/chat/agent-mode-model-defaults-api.ts` | `/agents/{id}/mode-model-defaults` | `listAgentModeModelDefaults`, `updateAgentModeModelDefault` |
| `lib/orchestration/orchestration-events.ts` | `/chats/{id}/orchestration` | `getOrchestration`, `subscribeOrchestrationEvents`, `kickOffAllOrchestration` |

## Existing API modules (query key mapping)

| Module | Path | Query keys |
|--------|------|------------|
| Chat | `lib/chat/api.ts` | `chatKeys` via `ChatProvider`, `useChatDetail` |
| Projects | `lib/projects/api.ts` | `projectKeys` via `ProjectProvider` |
| Agent models | `lib/chat/agent-models-api.ts` | `agentKeys.models` |
| Mode/model defaults | `lib/chat/agent-mode-model-defaults-api.ts` | `agentKeys.modeModelDefaults` |
| Orchestration | `lib/orchestration/orchestration-events.ts` | SSE + one-shot fetches (no query cache) |

## Optional: Zod runtime validation

Not required today. When adopted:

- Add `lib/{domain}/schemas.ts` with Zod schemas for DTOs
- Parse in `mapX()` with `Schema.parse(raw)` so invalid API responses fail loudly in dev
- Phase 1: `listChats` + `getChat`; phase 2: settings agent endpoints; phase 3: orchestration events

See [Plan 03 — Query keys & API standards](plans/03-query-keys-api-standards.md#optional-zod-runtime-validation).

## Related docs

- [TanStack Query](tanstack-query.md) — `useQuery`, cache, invalidation
- [HTTP & SSE infrastructure](plans/02-http-sse-infrastructure.md) — shared `read-error-message`, SSE parser
- [Chat streaming](chat-streaming.md) — SSE paths that bypass query cache
