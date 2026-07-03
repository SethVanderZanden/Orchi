# Plan: new-chat-dialog.tsx

**Status:** done  
**Path:** `src/renderer/src/components/chat/new-chat-dialog.tsx`

## Scope

Modal to create a chat in a workspace with agent mode selection.

## Component mapping

| Astryx | Replacement |
|--------|-------------|
| `Dialog` + `DialogHeader` + `Layout` | shadcn `Dialog`, `DialogHeader`, `DialogTitle`, `DialogDescription`, `DialogFooter` |
| `TextInput` (disabled agent) | `Input` + `Label` |
| `DropdownMenu` | shadcn `DropdownMenu` |
| `Button` | shadcn `Button` |
| `VStack` / `HStack` | Tailwind spacing |

## Behavior preserved

- Opens/closes via `open` / `onOpenChange`
- Submits `{ workspacePath, mode }` to `onCreateChat`
- Mode options: default, orchestration
- Orchestration helper text when selected
- Cancel closes dialog
- `isSubmitting` disables create button

## Verify

1. Create chat in default mode
2. Create chat in orchestration mode
3. Cancel does not create chat
4. Dialog closes after successful create
