# Chat streaming (SSE)

## Dummy section (start here)

Sending a chat message is like ordering pizza with **live status texts**:

1. "We got your order" (`status: processing`)
2. "Dough is rolling…" (`token` chunks — the reply appearing word by word)
3. "Adding toppings — Writing README.md" (`tool` rows)
4. "Out for delivery" (`done`)

If the oven breaks, you get an `error` text instead.

The desktop **does not** poll. It opens one HTTP response and reads events as they arrive (SSE).

**Orchi translation:**

| Pizza tracker | Orchi |
|---------------|-------|
| Order app | `sendMessageStream()` in `lib/chat/api.ts` |
| Live updates | SSE from `POST /chats/{id}/messages` |
| Order history | TanStack Query `chatKeys` + `ChatProvider` |
| "Still working" banner | Collapsed activity on assistant bubble |
| Tool steps | Collapsible list under active assistant turn |

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
| `tool` | `{ "label": "Reading README.md" }` | Separator `Marker` row |
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
| Electron app quit | Main process `POST /chats/shutdown` in `before-quit` |
| SSE abort | `AbortController` cancels in-flight stream; API cancels CLI |

## Further reading

- [TanStack Query](tanstack-query.md) — cache keys and mutations
- [Agent adapters](../agents/README.md) — backend streaming source
- [Cursor CLI](../agents/cursor-cli.md) — NDJSON → AgentEvent mapping
