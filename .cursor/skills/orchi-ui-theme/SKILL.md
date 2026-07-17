---
name: orchi-ui-theme
description: >-
  Keep Orchi desktop UI visually consistent with the neutral zinc shadcn theme,
  DM Sans / JetBrains Mono typography, and readable density. Use when changing
  theme tokens, fonts, colors, spacing, chrome layout, styling new screens,
  polishing UI, or when the user mentions theme, design system, T3-like look,
  or visual consistency.
---

# Orchi UI theme

## Source of truth

| What | Where |
|------|--------|
| Colors, radius, light/dark | `src/desktop/src/renderer/src/assets/main.css` (`:root` / `.dark`) |
| Font loading | `src/desktop/src/renderer/src/main.tsx` (fontsource imports) |
| Font stacks | `--font-sans` / `--font-mono` in `main.css` `@theme inline` |
| Component patterns | `docs/frontend/coding-standards.md` (Styling) |

Editing tokens in `main.css` updates the shadcn theme everywhere semantic classes are used.

## Do

- Use semantic Tailwind tokens: `bg-background`, `text-foreground`, `text-muted-foreground`, `border-border`, `bg-card`, `bg-primary`, `bg-muted`, `bg-accent`
- Keep light and dark token sets in sync when changing palette
- Prefer `font-sans` (default on body) and `font-mono` for code / kbd / technical chips
- Match nearby chrome density: headers ~`text-base` titles + `text-sm` secondary; content `max-w-3xl` / settings `max-w-2xl`
- Add shadcn primitives via CLI from `src/desktop/`: `npx shadcn@latest add <component>`

## Don’t

- Raw `hex` / `rgb` / `oklch` in TSX (exceptions: none for surfaces; status dots may use Tailwind named colors sparingly)
- Google Fonts or other CDN font links (CSP: `font-src 'self'`)
- A second design system (`@astryxdesign`, custom CSS modules for theming, purple gradient “AI” look)
- Ultra-tiny primary UI (`text-[10px]`) for titles or main actions
- Changing only light or only dark tokens and leaving the other mode broken

## Visual direction (short)

- Neutral zinc surfaces (near-black dark ≈ T3 Code; clean near-white light)
- Modest blue primary for focus rings / primary buttons only
- Soft borders, radius from `--radius` (~0.75rem base)
- Spacious but still a dense desktop app — more padding than before, not a marketing landing page

## Checklist before finishing UI work

- [ ] No new hex colors in components
- [ ] Uses existing tokens / type scale
- [ ] Light and dark still readable if tokens changed
- [ ] Fonts still self-hosted if font packages touched
