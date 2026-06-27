# Chat streaming (SSE)

## Dummy section (start here)

Sending a chat message is like ordering pizza with **live status texts**:

1. "We got your order" (`status: processing`)
2. "Dough is rolling…" (`token` chunks — the reply appearing word by word)
3. "Adding toppings — writeToolCall" (`tool` rows)
4. "Out for delivery" (`done`)

If the oven breaks, you get an `error` text instead.

The desktop **does not** poll. It opens one HTTP response and reads events as they arrive (SSE).

**Blazor version:** closest pattern is `HttpClient` with `ResponseHeadersRead` and manual stream reading, or SignalR hub pushing events to a component that appends to a `List<Message>`.

**Next.js version:** Client Component with `fetch` + `ReadableStream` reader (same as Orchi), or Route Handlers that proxy SSE — not Server Component streaming for bidirectional agent turns.

**Orchi translation:**

| Pizza tracker | Orchi |
|---------------|-------|
| Order app | `sendMessageStream()` in `lib/chat/api.ts` |
| Live updates | SSE from `POST /chats/{id}/messages` |
| Order history | TanStack Query `chatKeys` + `ChatProvider` |
| "Still working" banner | `Marker` component (`variant="default"`) |
| Tool steps | `Marker variant="separator"` |

---

## End-to-end flow

```
User submits message
  → ChatProvider.sendMessage()
  → POST /chats/{chatId}/messages (Accept: text/event-stream)
  → AgentSessionManager → CursorAgentAdapter → agent CLI
  → NDJSON parsed → AgentEvent → ChatSseWriter
  → Desktop SSE parser → update message bubble + markers
  → done → loadChat() reconciles with server state
```

## SSE event schema

Stable contract between API and desktop:

| Event | Data shape | UI effect |
|-------|------------|-----------|
| `status` | `{ "phase": "processing" }` | Show processing `Marker` |
| `token` | `{ "text": "..." }` | Append to assistant bubble; `status: streaming` |
| `tool` | `{ "name": "...", "status": "started\|completed", "detail": "..." }` | Separator `Marker` row |
| `done` | `{ "messageId": "..." }` | `status: complete`; clear processing markers |
| `error` | `{ "code": "...", "message": "..." }` | Error marker + message text |

Implemented in `src/API/Features/Chats/Shared/ChatSseWriter.cs`.

## Desktop client

`lib/chat/api.ts` — `sendMessageStream(chatId, content, handlers, signal)`:

- `fetch` POST with JSON body `{ content }`
- Reads `response.body` with a line buffer
- Parses `event:` / `data:` pairs
- Invokes typed handlers; supports `AbortSignal` for cancel

Proxy: `/chats` → API in `electron.vite.config.ts` (dev).

## ChatProvider integration

`providers/chat-provider.tsx`:

- **`useQuery`** — `chatKeys.lists()` for sidebar
- **`useMutation`** — create / close chat
- **`sendMessage`** — optimistic user message + assistant placeholder; SSE handlers update cache via `queryClient.setQueryData`
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

Render order during an active turn:

1. Message bubbles (user + assistant)
2. Tool markers (`variant="tool"`) — chronological tool steps
3. Status marker (`variant="status"`) — **"Agent is working…" pinned last**

- **`variant="default"`** — spinner + "Agent is working…" while processing
- **`variant="separator"`** — human-readable tool label via `formatToolMarker()` (e.g. "Reading README.md")
- **`MessageScroller`** keeps scroll anchor on the last item (status marker when streaming)

Tool labels are formatted in `lib/chat/format-tool-marker.ts` from SSE `tool` events.

## Assistant message display

`components/chat/assistant-message-content.tsx`:

| Status | Display |
|--------|---------|
| `processing` / `streaming` | Plain text (`whitespace-pre-wrap`) — avoids broken partial markdown |
| `complete` | `react-markdown` + `remark-gfm` (headings, lists, tables, code) |

Text normalization (`lib/chat/normalize-agent-text.ts`) repairs common UTF-8 mojibake (e.g. `ΓÇö` → em dash) on tokens and after `loadChat()`.

The API reads Cursor CLI stdout as **UTF-8** (`StandardOutputEncoding` on `ProcessStartInfo`) to prevent garbled punctuation on Windows.

## Create chat

Desktop starts with **zero chats**. **New chat** dialog (`new-chat-dialog.tsx`) collects **workspace path** (required); agent is fixed to Cursor in v1. `POST /chats` then navigates to `/chat/$chatId`.

## Cleanup

| Trigger | Action |
|---------|--------|
| User closes chat | `DELETE /chats/{id}` via `closeChat()` |
| Electron app quit | Main process `POST /chats/shutdown` in `before-quit` |
| SSE abort | `AbortController` cancels in-flight stream; API cancels CLI |

## Further reading

- [TanStack Query](tanstack-query.md) — cache keys and mutations
- [Agent adapters](../agents/README.md) — backend streaming source
- [Cursor CLI](../agents/cursor-cli.md) — NDJSON → AgentEvent mapping
