# Plan: shadcn component inventory

**Status:** done  
**Base:** `src/desktop/components.json`

All components below are initialized and ready for use across the app.

## Core UI (`components/ui/`)

| Component | Radix / deps | Used by |
|-----------|--------------|---------|
| `button` | `@radix-ui/react-slot`, CVA | navigator, settings, dialogs, composer, plans |
| `input` | native | navigator search, settings, dialog |
| `label` | `@radix-ui/react-label` | settings, dialog |
| `textarea` | native | chat composer |
| `dialog` | `@radix-ui/react-dialog` | new chat |
| `dropdown-menu` | `@radix-ui/react-dropdown-menu` | new chat mode |
| `avatar` | `@radix-ui/react-avatar` | assistant messages |
| `badge` | CVA | orchestration mode tag |
| `card` | — | settings, plan cards |
| `scroll-area` | `@radix-ui/react-scroll-area` | navigator chat list |
| `separator` | `@radix-ui/react-separator` | navigator, settings |
| `collapsible` | `@radix-ui/react-collapsible` | tool calls |
| `tooltip` | `@radix-ui/react-tooltip` | navigator icon buttons |
| `page-header` | — | navigator, chat, settings headers |

## Supporting libraries

| Package | Purpose |
|---------|---------|
| `lucide-react` | Icons (replaces heroicons + Astryx Icon) |
| `react-markdown` + `remark-gfm` | Assistant message rendering |
| `date-fns` | Relative timestamps |
| `class-variance-authority` | Button/badge variants |
| `clsx` + `tailwind-merge` | `cn()` utility |
| `@tailwindcss/typography` | Markdown prose styles |

## Adding more components

From `src/desktop`:

```bash
npx shadcn@latest add <component-name>
```

Suggested if features expand: `sheet`, `command`, `popover`, `tabs`, `sonner` (toasts).
