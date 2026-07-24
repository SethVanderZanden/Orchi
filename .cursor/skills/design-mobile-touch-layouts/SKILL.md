---
name: design-mobile-touch-layouts
description: Create or review mobile layouts for reliable touch input, reachability, action placement, responsive reflow, and constrained viewports. Use when mobile controls are difficult to reach or tap, actions crowd the screen, or desktop patterns do not translate to touch.
---

# Design Mobile Touch Layouts

## Map the mobile task

1. Identify the most frequent and important actions for the current task.
2. Note one-handed use, posture, device size, orientation, safe areas, and software-keyboard effects.
3. Separate persistent actions from contextual and infrequent actions.
4. Preserve the logical reading and focus order before choosing visual placement.

## Place actions deliberately

- Put frequent actions where they are discoverable and reasonably reachable for the target context.
- Keep actions close to the content they affect when that reduces ambiguity.
- Use persistent bottom or floating actions only when their importance justifies the occupied space.
- Keep destructive or conflicting controls separated from frequent actions.
- Adapt navigation and toolbars rather than squeezing desktop controls into one row.

## Make touch reliable

- Meet the current applicable accessibility and platform target-size guidance.
- Include enough separation to prevent adjacent activation.
- Make the interactive hit area match or exceed the visible affordance.
- Provide pressed, selected, disabled, loading, and focus states.
- Offer alternatives for precision gestures, drag, hover, multi-touch, or motion.

## Test constrained states

- Test narrow and wide phones, landscape, zoom, text scaling, long labels, and localization.
- Test with the software keyboard visible and fields near viewport edges.
- Check safe-area insets, sticky regions, scrolling, browser chrome, and system gestures.
- Verify that reordering for reachability does not change meaning or assistive-technology order.

## Avoid these failures

- Do not treat a fixed "thumb zone" diagram as universal.
- Do not hide core actions behind gestures alone.
- Do not shrink targets merely to keep one-line toolbars.
- Do not make persistent controls cover content, errors, or focused fields.
- Do not rely on hover for instructions or state.

## Produce the result

Return:

1. A mobile action-priority map.
2. Placement and persistence decisions.
3. Target-size, spacing, and interaction-state requirements.
4. Responsive, keyboard, safe-area, and gesture behavior.
5. A device and input test matrix.
