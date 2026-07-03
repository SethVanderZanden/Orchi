# Plan: chat surface (composer, messages, panel)

**Status:** done  
**Paths:**
- `components/chat/chat-composer.tsx`
- `components/chat/chat-message-list.tsx`
- `components/chat/chat-panel.tsx`
- `components/chat/chat-layout.tsx`
- `components/chat/chat-tool-calls.tsx`
- `components/layout/chat-workspace-panel.tsx`

## Scope

Full chat UX: header metadata, message thread, composer, orchestration plan area hook-in.

## Component mapping

| Astryx | Replacement |
|--------|-------------|
| `ChatLayout` | `ChatLayout` (scroll + bordered composer footer) |
| `ChatComposer` | `form` + `Textarea` + send `Button` |
| `ChatMessageList` | flex column with max-width container |
| `ChatMessage` / `ChatMessageBubble` | role-based bubble styling |
| `Markdown` | `MarkdownContent` (react-markdown) |
| `Avatar` | shadcn `Avatar` + fallback initials |
| `ChatToolCalls` | `Collapsible` + status dots |
| `EmptyState` | `EmptyState` component |
| `Toolbar` | `PageHeader` |
| `Token` | `Badge` |

## Behavior preserved

- Enter sends (Shift+Enter for newline)
- Disabled composer while sending or kicking off plan
- User messages right-aligned primary bubble
- Assistant: processing placeholder, streaming plain text, complete markdown
- Error state border on assistant bubble
- Tool markers only on active assistant turn
- Orchestration badge and plan file path in header

## UI cleanup

- Centered max-w-3xl message column
- Cleaner tool-call collapsible
- Send icon button with aria-label

## Verify

1. Empty chat shows empty state
2. Send user message clears composer
3. Assistant streaming shows content incrementally
4. Tool markers appear during active turn
5. Orchestration chats show plan cards below messages
