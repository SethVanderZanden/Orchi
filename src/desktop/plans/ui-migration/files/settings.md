# Plan: settings.tsx

**Status:** done  
**Path:** `src/renderer/src/routes/_app/settings.tsx`

## Scope

Project management: list, add via picker or path, rename inline, remove, navigate back to chats.

## Component mapping

| Astryx | Replacement |
|--------|-------------|
| `Layout` + `Toolbar` | flex column + `PageHeader` |
| `Section` | `Card` |
| `List` / `ListItem` | bordered `<ul>` with row actions |
| `TextInput` | `Input` + `Label` |
| `Button` | shadcn `Button` |
| `Icon` + TrashIcon | lucide `Trash2` |

## Behavior preserved

- Pick directory adds workspace
- Manual path add with validation via `displayWorkspacePath`
- Click project row starts inline rename; blur saves
- Remove project (stopPropagation not needed — separate button)
- “Open chats” navigates to `/`

## UI cleanup

- Centered max-w-2xl content
- Card-based sections with clear hierarchy
- Icon-only delete with aria-label

## Verify

1. Add project via button and manual path
2. Rename project on click + blur
3. Remove project from list
4. Open chats returns to index
