---
name: craft-readable-typography
description: Create or review an interface type system covering font choice, hierarchy, line length, line height, weight, alignment, and responsive text behavior. Use when readability, scanning, text density, heading structure, or inconsistent type styles are the main concern.
---

# Craft Readable Typography

## Start with content and constraints

1. Identify languages, scripts, content types, reading length, platforms, and brand constraints.
2. Reuse available typefaces and tokens unless a change solves a demonstrated problem.
3. Confirm required characters, weights, styles, loading behavior, and licensing.
4. Define semantic text roles before choosing exact sizes.

## Build the type system

- Create a restrained scale for display, headings, body, labels, controls, captions, and data.
- Use size, weight, spacing, and placement together to express hierarchy.
- Give body text enough line height and measure for comfortable reading.
- Keep controls and labels legible under zoom and content growth.
- Use alignment that supports the content and reading direction; reserve centered text for short, simple passages.
- Use real font metrics rather than assuming equal nominal sizes render equally.

## Tune composition

- Adjust line length by context; treat common character-count ranges as starting heuristics, not fixed standards.
- Avoid excessive weights, all-caps passages, forced letter spacing, and low-contrast metadata.
- Pair typefaces only when the distinction has a clear role.
- Preserve hierarchy at narrow widths without shrinking text indiscriminately.
- Prevent clipped glyphs, orphaned labels, overlapping text, and layout dependence on one line.

## Verify usability

- Test zoom, reflow, browser or OS text scaling, and user font preferences where applicable.
- Review realistic long names, translated strings, numbers, symbols, and error text.
- Check logical heading structure and accessible names separately from visual styling.
- Evaluate loading fallbacks and layout shift for web fonts.

## Avoid these failures

- Do not treat 16 pixels, two typefaces, or a specific line length as universal law.
- Do not use font size alone to create hierarchy.
- Do not replace meaningful labels with typography or icons alone.
- Do not justify poor contrast as subtlety.
- Do not sacrifice reading comfort to fit a fixed-height component.

## Produce the result

Return:

1. Semantic type roles and proposed tokens.
2. Font, size, weight, line-height, measure, and alignment decisions.
3. Hierarchy and readability problems with concrete corrections.
4. Responsive, localization, and zoom checks.
5. Any assumptions requiring content or user validation.
