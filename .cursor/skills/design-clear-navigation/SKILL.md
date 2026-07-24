---
name: design-clear-navigation
description: Create or review menus, dropdowns, tabs, breadcrumbs, and information architecture using recognizable labels, grouping, and location cues. Use when users struggle to find destinations, understand menu categories, orient themselves, or operate responsive navigation.
---

# Design Clear Navigation

## Model destinations and tasks

1. Inventory destinations, user goals, frequency, importance, permissions, and relationships.
2. Group items by the user's mental model rather than internal departments or implementation.
3. Name destinations with concise, recognizable language.
4. Separate navigation to places from commands that change data.

## Choose the pattern

- Use persistent navigation for frequent top-level destinations.
- Use tabs for peer views within one context, not unrelated routes.
- Use breadcrumbs when hierarchy and return paths matter.
- Use dropdowns or disclosure for manageable related choices, not as a dumping ground.
- Use search as a complement to coherent structure, not a substitute.
- Keep utility, account, and destructive actions distinct from primary navigation.

## Clarify state and behavior

- Show the current location and expanded state through more than color alone.
- Make parent-child relationships visible through grouping, indentation, headings, or sequence.
- Keep labels, ordering, and icons consistent across breakpoints.
- Define keyboard, focus, escape, click-outside, and submenu behavior.
- Preserve user context when switching views where appropriate.

## Adapt responsively

- Prioritize destinations instead of indiscriminately hiding them.
- Use a mobile pattern that preserves labels and hierarchy.
- Avoid deep cascades that are difficult with touch, keyboard, or zoom.
- Test long labels, localization, permissions, notifications, and dynamic item counts.
- Ensure collapsed navigation remains discoverable and announces its state.

## Avoid these failures

- Do not use vague labels such as "More" when a stable category name is available.
- Do not mix actions and destinations without clear treatment.
- Do not depend on icons alone for unfamiliar destinations.
- Do not make hover the only way to open essential navigation.
- Do not reorganize labels between desktop and mobile without a strong reason.

## Produce the result

Return:

1. A destination and task inventory.
2. The proposed grouping, order, and labels.
3. The selected navigation patterns and current-location cues.
4. Keyboard, touch, responsive, and permission behavior.
5. Findability tests for representative user goals.
