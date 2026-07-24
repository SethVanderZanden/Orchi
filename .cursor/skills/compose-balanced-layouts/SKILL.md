---
name: compose-balanced-layouts
description: Create or review interface composition through grids, alignment, proportion, scale, rhythm, balance, and responsive behavior. Use when page structure, column relationships, responsive reflow, visual balance, or cross-screen layout consistency is the central problem.
---

# Compose Balanced Layouts

## Establish the structure

1. Identify the primary content, supporting content, controls, and persistent regions.
2. Choose a content order that remains meaningful without the visual grid.
3. Reuse the product's existing container, grid, breakpoint, and alignment rules.
4. Define deliberate anchors for major edges, baselines, and centers.

## Compose the layout

- Size regions according to content and task priority.
- Use consistent columns and gutters while allowing intentional exceptions.
- Align related elements to shared edges or baselines.
- Balance visual weight across the composition; balance may be symmetrical or asymmetrical.
- Repeat proportions, spacing, and shapes to create rhythm without monotony.
- Keep line lengths and dense controls within usable bounds.

## Design responsive behavior

- Define how each region resizes, wraps, stacks, moves, collapses, or becomes scrollable.
- Preserve task order and context as space decreases.
- Prefer natural reflow over scaling an entire desktop composition down.
- Test intermediate widths, not only named device presets.
- Account for long labels, localization, zoom, dynamic data, software keyboards, and safe areas.

## Review alignment and harmony

- Detect nearly aligned edges, accidental centering, and unrelated elements sharing an anchor.
- Check whether oversized decoration or empty regions distort the balance.
- Verify that repeated cards and rows tolerate unequal content.
- Ensure layout changes do not change meaning or keyboard sequence unexpectedly.

## Avoid these failures

- Do not force every element onto a grid when the exception improves meaning.
- Do not center long-form text or complex controls by default.
- Do not use symmetry when it hides priority.
- Do not assume a visually balanced screenshot will remain balanced with real data.
- Do not encode responsive behavior as a single desktop-to-mobile jump.

## Produce the result

Provide:

1. A content-order and region map.
2. Grid, alignment, and proportion decisions.
3. Responsive behavior for each major region.
4. Detected balance or rhythm problems with concrete corrections.
5. Content and viewport stress tests.
