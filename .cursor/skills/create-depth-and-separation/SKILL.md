---
name: create-depth-and-separation
description: Create or review intentional surface separation using backgrounds, borders, shadows, overlap, elevation, blur, and texture. Use when layers, cards, dialogs, sticky regions, glass effects, or section boundaries feel flat, noisy, ambiguous, or visually heavy.
---

# Create Depth and Separation

## Map surfaces and layers

1. Identify the base canvas, sections, containers, floating controls, overlays, dialogs, and transient feedback.
2. State which surfaces are peers, nested, interactive, elevated, or modal.
3. Use the least decoration that communicates those relationships clearly.
4. Align visual elevation with interaction and stacking behavior.

## Choose separation methods

- Use whitespace for ordinary grouping.
- Use a background change for broad regions or nested surfaces.
- Use borders for crisp boundaries and dense components.
- Use shadows for elevation, overlap, or floating elements.
- Use texture or blur only when it supports hierarchy and keeps content legible.
- Combine methods sparingly; one clear cue is often enough.

## Build a coherent depth system

- Define a small set of elevation levels and reuse them.
- Increase shadow spread, softness, and opacity deliberately rather than randomly.
- Keep the perceived light source consistent.
- Provide visible boundaries when shadows disappear in dark themes, high contrast, print, or low-quality displays.
- Match corner treatment and clipping to component ownership.

## Verify interaction and access

- Ensure overlays do not reduce foreground contrast below requirements.
- Preserve focus indicators and visible boundaries.
- Check that z-index, focus trapping, pointer blocking, and reading order match the apparent layer.
- Respect reduced-transparency or reduced-motion preferences where relevant.
- Test scroll, sticky, and overlapping content at multiple widths.

## Avoid these failures

- Do not give every component a shadow.
- Do not use glass effects over uncontrolled imagery without a robust contrast treatment.
- Do not imply clickability through elevation on noninteractive surfaces.
- Do not use decorative depth to compensate for weak grouping.
- Do not create a visual layer that behaves like a peer in focus or stacking order.

## Produce the result

Provide:

1. A surface and elevation map.
2. The chosen separation cue for each relationship.
3. Reusable border, background, shadow, and overlay rules.
4. Dark-theme and accessibility checks.
5. Layering behaviors that implementation must preserve.
