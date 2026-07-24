---
name: design-accessible-color
description: Create or review a restrained color system with semantic roles and verified contrast. Use for palette creation, theming, dark mode, status colors, contrast audits, color-token design, and interfaces that rely on color to communicate meaning.
---

# Design Accessible Color

## Define roles before values

1. Inventory backgrounds, surfaces, text, borders, actions, focus, selection, and semantic states.
2. Assign each color a role rather than coupling components to raw values.
3. Start from existing brand and design-system tokens.
4. Limit accents so emphasis remains meaningful.

## Build the system

- Create neutral scales for surfaces, text, and borders.
- Create interaction states for rest, hover, active, focus, selected, and disabled.
- Create semantic roles for success, warning, error, and informational states.
- Pair every semantic color with text, shape, iconography, pattern, or position when meaning matters.
- Define light and dark themes independently enough to preserve hierarchy and contrast.
- Test real combinations; do not infer contrast from palette swatches.

## Verify accessibility

- Use the current applicable accessibility standard and an actual contrast calculation.
- For WCAG 2.2 AA, verify at least 4.5:1 for normal text and 3:1 for large text.
- Verify at least 3:1 where WCAG requires contrast for meaningful non-text UI components and graphical objects.
- Do not use color as the only visual means of conveying information, prompting a response, or distinguishing an element.
- Check focus indicators, links in prose, placeholder text, disabled states, charts, and validation messages.
- Test color-vision variations and forced-colors or high-contrast modes when supported.

## Account for context

- Treat cultural or emotional color associations as hypotheses, not universal meanings.
- Preserve brand character through proportion and placement, not indiscriminate saturation.
- Consider glare, ambient light, display quality, overlays, opacity, and imagery behind text.
- Allow user or system theme preferences without claiming every product requires a dark theme.

## Avoid these failures

- Do not declare colors accessible by appearance alone.
- Do not assume pure black or white is inherently inaccessible.
- Do not encode status only through red, green, or hue.
- Do not make secondary text unreadable to create hierarchy.
- Do not reuse one token for unrelated semantic purposes.

## Produce the result

Provide:

1. A role-based palette or token map.
2. The exact foreground-background pairs checked and their results.
3. Non-color cues for meaningful states.
4. Theme and interaction-state behavior.
5. Unverified combinations or platform constraints.
