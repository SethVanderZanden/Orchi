# Plan: workspace-navigator.tsx

**Status:** done  
**Path:** `src/renderer/src/components/workspace/workspace-navigator.tsx`

## Scope

Left sidebar: branding, project tree, chat list, search, new-chat dialog trigger, settings link.

## Component mapping

| Astryx | Replacement |
|--------|-------------|
| `Layout` + `LayoutContent` | `<aside>` + flex column |
| `Toolbar` | `PageHeader` |
| `HStack` / `VStack` | Tailwind flex/gap |
| `TextInput` + search icon | `Input` with lucide `Search` |
| `List` / `ListItem` | `<button>` rows with hover/active states |
| `Button` / `IconButton` | shadcn `Button` (variant ghost/secondary) |
| `Icon` + heroicons | lucide-react icons |
| `Timestamp` | `RelativeTime` |
| `Section` | padded divs + `Separator` |
| `NewChatDialog` | unchanged API, shadcn dialog inside |

## Behavior preserved

- Expand/collapse project groups (`useWorkspaceLayout`)
- Auto-expand active chat’s project
- Filter via `searchQuery`
- Orphan workspace “Add as project”
- New chat opens `NewChatDialog` per project
- Settings navigation + `aria-current`
- Loading and empty workspace states

## UI cleanup

- Fixed 240px → `w-60` sidebar
- Tooltips on icon-only actions
- Clear active chat styling via `bg-accent`
- Nested chats with left border indent

## Verify

1. Add project from header icon and footer button
2. Search filters chat titles
3. Click chat navigates to `/chat/$chatId`
4. New chat button opens dialog with mode selector
5. Settings icon navigates to `/settings`
