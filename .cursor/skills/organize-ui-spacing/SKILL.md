---
name: organize-ui-spacing
description: Create or review spacing relationships using proximity, whitespace, grouping, and a consistent spacing system. Use when interfaces feel cramped, disconnected, uneven, hard to scan, or dependent on arbitrary margins and padding.
---

# Organize UI Spacing

## Map relationships

1. Group elements by task and meaning before changing measurements.
2. Identify parent-child, peer, and unrelated relationships.
3. Make related items closer than unrelated groups.
4. Use whitespace to create boundaries before adding containers, rules, or shadows.

## Establish a spacing system

- Start from the project's existing tokens or scale.
- Use a small, repeatable set of increments rather than one-off values.
- Assign spacing by relationship: inline, within control, within component, between groups, and between sections.
- Keep internal component spacing tighter than the space around the component.
- Align repeated gaps across comparable structures.
- Preserve enough breathing room for reading, focus indicators, touch use, and content growth.

## Review the composition

- Inspect vertical rhythm from page start to completion.
- Compare equivalent components and states for spacing drift.
- Check dense areas separately from spacious marketing or reading layouts.
- Test narrow widths, wrapping labels, validation messages, empty values, and expanded content.
- Verify that whitespace does not push critical context or actions unnecessarily far apart.

## Choose the right separator

- Use whitespace for ordinary grouping.
- Add a subtle background or border when whitespace alone cannot explain ownership or state.
- Use a stronger container only when users need persistent boundaries.
- Avoid stacking whitespace, borders, shadows, and backgrounds to express the same relationship.

## Avoid these failures

- Do not apply equal gaps to relationships of different strength.
- Do not add space without checking alignment and content order.
- Do not compress controls below usable pointer or touch dimensions.
- Do not treat whitespace as wasted space or maximal space as inherently elegant.
- Do not invent a new scale when an established design system already covers the need.

## Produce the result

Return:

1. A grouping map.
2. The spacing inconsistencies and their user impact.
3. Proposed tokens or relationship-based spacing rules.
4. Component, section, and responsive changes.
5. Edge cases to verify with real content.
