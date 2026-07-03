# UI migration: Astryx → shadcn

Track file-by-file replacement of `@astryxdesign/*` with shadcn/ui and Tailwind primitives.

## Status legend

- **done** — migrated and verified
- **n/a** — removed or not part of the app shell

## Infrastructure

| File | Status | Notes |
|------|--------|-------|
| `components.json` | done | shadcn config (new-york, neutral, lucide) |
| `src/renderer/src/lib/utils.ts` | done | `cn()` helper |
| `src/renderer/src/assets/main.css` | done | shadcn CSS variables, dark theme, typography plugin |
| `package.json` | done | Removed `@astryxdesign/*`, `@heroicons/react`, `astryx` script |
| `providers/theme-provider.tsx` | done | Dark class wrapper (replaces Astryx Theme) |

## shadcn components initialized

| Component | Path |
|-----------|------|
| avatar | `components/ui/avatar.tsx` |
| badge | `components/ui/badge.tsx` |
| button | `components/ui/button.tsx` |
| card | `components/ui/card.tsx` |
| collapsible | `components/ui/collapsible.tsx` |
| dialog | `components/ui/dialog.tsx` |
| dropdown-menu | `components/ui/dropdown-menu.tsx` |
| input | `components/ui/input.tsx` |
| label | `components/ui/label.tsx` |
| page-header | `components/ui/page-header.tsx` |
| scroll-area | `components/ui/scroll-area.tsx` |
| separator | `components/ui/separator.tsx` |
| textarea | `components/ui/textarea.tsx` |
| tooltip | `components/ui/tooltip.tsx` |

## Custom replacements (no direct shadcn equivalent)

| File | Status | Replaces |
|------|--------|----------|
| `components/chat/chat-layout.tsx` | done | `ChatLayout` |
| `components/chat/chat-tool-calls.tsx` | done | `ChatToolCalls` |
| `components/chat/chat-composer.tsx` | done | `ChatComposer`, `ChatComposerInput` |
| `components/chat/chat-message-list.tsx` | done | `ChatMessage*`, `EmptyState`, `Markdown`, `Avatar` |
| `components/markdown-content.tsx` | done | `Markdown` (react-markdown + remark-gfm) |
| `components/relative-time.tsx` | done | `Timestamp` (date-fns) |
| `components/empty-state.tsx` | done | `EmptyState` |

## App shell & routes

| File | Status | Astryx → shadcn mapping |
|------|--------|-------------------------|
| `components/layout/app-layout.tsx` | done | `Layout`/`HStack` → flex divs |
| `components/layout/workspace-shell-layout.tsx` | done | already plain divs |
| `components/layout/chat-workspace-panel.tsx` | done | `Toolbar`→`PageHeader`, `Token`→`Badge`, `VStack`→flex |
| `components/workspace/workspace-navigator.tsx` | done | `List`/`ListItem`→buttons, `TextInput`→`Input`, icons→lucide |
| `components/chat/chat-panel.tsx` | done | `ChatLayout`→local component |
| `components/chat/new-chat-dialog.tsx` | done | `Dialog`/`DropdownMenu`→shadcn |
| `components/orchestration/plan-cards.tsx` | done | `Card`/`Button`→shadcn |
| `routes/_app/index.tsx` | done | `VStack`/`Text`→Tailwind |
| `routes/_app/chat.$chatId.tsx` | done | loading state typography |
| `routes/_app/settings.tsx` | done | `Section`/`List`→`Card` + list markup |

## Removed (not app functionality)

| File | Status | Notes |
|------|--------|-------|
| `_templates/ai-chat/page.tsx` | n/a | Astryx demo template; deleted |
| `_templates/file-explorer/page.tsx` | n/a | Astryx demo template; deleted |

## Functionality checklist

Use this when verifying the migration:

- [ ] Navigator: search, expand/collapse projects, select chat, new chat dialog
- [ ] Navigator: add project (header + footer), orphan “Add as project”
- [ ] Navigator: settings link, active chat highlight
- [ ] Chat: send message (Enter), disabled while sending/kicking off
- [ ] Chat: user/assistant bubbles, streaming/processing/error states
- [ ] Chat: tool call markers, empty state
- [ ] Orchestration: plan cards, kick off button states
- [ ] Settings: add/remove/rename projects, manual path, open chats
- [ ] Routing: index redirect, invalid chat redirect, load chat on navigate

## Optional follow-ups

- Theme toggle (light/dark) via `next-themes` instead of fixed dark
- Resizable panels if artifact/split views are added later
- Code blocks in markdown (syntax highlighting)
