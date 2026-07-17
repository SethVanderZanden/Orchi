# Chat streaming (SSE)

## Dummy section (start here)

Sending a chat message is like ordering pizza with **live status texts**:

1. "We got your order" (`status: processing`)
2. "Dough is rolling…" (`token` chunks — the reply appearing word by word)
3. "Adding toppings — Writing README.md" (`tool` rows)
4. "Out for delivery" (`done`)

If the oven breaks, you get an `error` text instead.

The desktop **does not** poll. It opens one HTTP response and reads events as they arrive (SSE).

The **sidebar dots** are a separate mailroom light board owned by the API:

| Light | Meaning | `ChatStatus` |
|-------|---------|--------------|
| Gray | You've seen it | `read` |
| Pulsing | Being written | `inProgress` |
| Solid primary | Something new to look at | `readyForReview` |

**Aha:** The UI only paints what the server says; it does not keep its own "have I seen this?" sticky notes.

**Orchi translation:**

| Pizza / mailroom | Orchi |
|------------------|-------|
| Order app | `sendMessageStream()` in `lib/chat/api.ts` |
| Live reply updates | SSE from `POST /chats/{id}/messages` |
| Light board | `Chat.Status` + `GET /chats/status/events` |
| "I've looked at this" stamp | `POST /chats/{id}/read` |
| Order history | TanStack Query `chatKeys` + `ChatProvider` |

Everything below is the same idea with event names and hooks.

---

## End-to-end flow

```
User submits message
  → ChatProvider.sendMessage()  (via useChatStream hook)
  → POST /chats/{chatId}/messages (Accept: text/event-stream)
  → AgentSessionManager → CursorAgentAdapter → agent CLI
  → NDJSON parsed → AgentEvent → ChatSseWriter
  → readSseStream (lib/http/sse.ts) → message-stream-handlers → Query cache + markers
  → done → loadChat() reconciles with server state
```

Streaming orchestration is split: **`useChatStream`** (`hooks/chat/use-chat-stream.ts`) owns SSE handlers, markers, and abort; **`ChatProvider`** composes it with list/cache/mutation hooks.

## Chat status (sidebar)

Server-owned enum on each chat (`read` | `inProgress` | `readyForReview`):

| Transition | Status |
|------------|--------|
| Agent turn starts | `inProgress` |
| Assistant turn completes or errors | `readyForReview` |
| `POST /chats/{id}/read` while idle | `read` (+ `LastReadAt`) |
| Mark-read while still running | stays `inProgress`; `LastReadAt` updates |

Live board: `GET /chats/status/events`

| Event | Data | Client effect |
|-------|------|---------------|
| `snapshot` | `[{ chatId, status }, …]` | Seed list/detail caches |
| `status` | `{ chatId, status }` | Patch sidebar dots |

Desktop: `subscribeChatStatusEvents` + `useChatStatusEvents`; active chat auto-calls `markChatRead`. Dot mapping lives in `lib/chat/chat-sidebar-status.ts`.

## SSE event schema

Stable contract between API and desktop:

| Event | Data shape | UI effect |
|-------|------------|-----------|
| `status` | `{ "phase": "processing" }` | Show processing `Marker` |
| `token` | `{ "text": "..." }` | Append to assistant bubble; `status: streaming` |
| `tool` | `{ "label": "Reading README.md" }` | Separator `Marker` row |
| `done` | `{ "messageId": "..." }` | `status: complete`; clear processing markers |
| `error` | `{ "code": "...", "message": "..." }` | Error marker + message text |

Implemented in `src/API/Features/Chats/Shared/ChatSseWriter.cs`.

### Orchestration SSE (parent chat)

When viewing an orchestration chat, the desktop subscribes to `GET /chats/{parentChatId}/orchestration/events`. The API multiplexes workflow updates and child agent activity onto one stream:

| Event | Data shape | UI effect |
|-------|------------|-----------|
| `workflow` | `{ status, currentStep?, totalSteps?, planId? }` | Plan cards progress ("Running plan 2 of 3…") |
| `chat_created` | `{ chatId, mode, parentChatId, planId?, planFilePath? }` | Insert child chat in sidebar/cache |
| `parent_message` | `{ messageId, role, content }` | Append status row on orchestration parent |
| `agent_status` | `{ childChatId, phase }` | Child processing indicator |
| `agent_token` | `{ childChatId, text }` | Append to child chat cache |
| `agent_tool` | `{ childChatId, label }` | Tool activity on child chat |
| `agent_done` | `{ childChatId, messageId, succeeded }` | Mark child turn complete |
| `agent_error` | `{ childChatId, code, message }` | Child error state |

Client: `lib/orchestration/orchestration-events.ts` + `hooks/use-orchestration.ts`.

## Desktop client

Shared SSE parsing: `lib/http/sse.ts` — `parseSseBlock()` and `readSseStream()` for any streaming `fetch` response.

`lib/chat/api.ts` — `sendMessageStream(chatId, content, handlers, signal)`:

- `fetch` POST with JSON body `{ content }`
- Delegates body reading to `readSseStream` from `lib/http/sse.ts`
- Invokes typed handlers; supports `AbortSignal` for cancel

Also: `markChatRead(chatId)`, `subscribeChatStatusEvents(handlers, signal)`.

Orchestration uses the same parser in `lib/orchestration/orchestration-events.ts` (`subscribeOrchestrationEvents`).

Proxy: `/chats` → API in `electron.vite.config.ts` (dev).

## ChatProvider integration

`providers/chat-provider.tsx` composes focused hooks; streaming lives in **`hooks/chat/use-chat-stream.ts`**:

| Hook / module | Role |
|---------------|------|
| `useChatList` | Sidebar list query (`chatKeys.lists()`) |
| `useChatCache` | Detail load, `getChat`, optimistic cache updates |
| `useChatStream` | `sendMessage`, SSE handlers, `markersByChat`, abort |
| `useChatMutations` | create / close / mode / model mutations |
| `useChatStatus` | mark-read on active chat, sidebar status from server |
| `useChatStatusEvents` | app-wide status SSE → query cache |

**`useChatStream`** specifics:

- Optimistic user message + assistant placeholder on send
- SSE handlers update cache via `queryClient.setQueryData` and `createMessageStreamHandlers`
- **`markersByChat`** — ephemeral UI rows (processing spinner, tool lines)

Query keys (`lib/query-keys.ts`):

```ts
export const chatKeys = {
  all: ['chats'] as const,
  lists: () => [...chatKeys.all, 'list'] as const,
  detail: (chatId: string) => [...chatKeys.all, 'detail', chatId] as const
}
```

## Marker UI

`components/chat/chat-message-list.tsx`:

During an active turn, tool and status markers attach to the **last assistant bubble** — not as separate scroll rows:

- **Collapsed by default** — shows `"N steps"` with a wrench icon (or `"Working…"` before any tool events)
- **Expand** — chronological tool labels from SSE
- **Scroll anchor** stays on the assistant message bubble so streaming text stays visible

Tool labels are formatted by the agent adapter (e.g. `CursorToolLabelFormatter`) before SSE; the desktop displays `label` as-is. Markers clear on `done` or `error`.

## Assistant message display

`components/chat/assistant-message-content.tsx`:

| Status | Display |
|--------|---------|
| `processing` / `streaming` | Plain text (`whitespace-pre-wrap`) — avoids broken partial markdown |
| `complete` | `react-markdown` + `remark-gfm` (headings, lists, tables, code) |

The API reads Cursor CLI stdout as **UTF-8** (`StandardOutputEncoding` on `ProcessStartInfo`) to prevent garbled punctuation on Windows.

## Create chat

Desktop starts with **zero chats**. Register **project folders** in Settings or via **Add project** in the sidebar. Chats are grouped by project; each group has a `(+)` button that opens a workspace-scoped new-chat dialog. Agent is Cursor in v1. `POST /chats` then navigates to `/chat/$chatId`.

## Project workspaces

Projects are a **desktop-only** registry (`localStorage` + folder picker IPC). The API stores `workspacePath` per chat; the sidebar groups chats by registered project paths. Chats whose path is not registered appear under **Other**.

## Cleanup

| Trigger | Action |
|---------|--------|
| User closes chat | `DELETE /chats/{id}` via `closeChat()` |
| Electron app quit | Main process `POST /chats/shutdown` (10s timeout), then stops bundled `Orchi.Api.exe` in production (15s total shutdown budget) |
| SSE abort | `AbortController` cancels in-flight stream; API cancels CLI |

## Further reading

- [TanStack Query](tanstack-query.md) — cache keys and mutations
- [HTTP & SSE infrastructure](plans/02-http-sse-infrastructure.md) — shared SSE parser
- [Agent adapters](../agents/README.md) — backend streaming source
- [Cursor CLI](../agents/cursor-cli.md) — NDJSON → AgentEvent mapping
