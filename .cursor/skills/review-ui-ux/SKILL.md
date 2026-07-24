---
name: review-ui-ux
description: Review an existing or proposed interface holistically and prioritize concrete UI and UX improvements. Use for design critiques, redesign planning, pre-release audits, screenshot or prototype reviews, and requests that span several concerns rather than one specialized design problem.
---

# Review UI and UX

## Establish the review frame

1. Identify the interface, target users, primary task, platform, constraints, and evidence available.
2. Inspect the actual artifact when possible. Review all relevant screens, states, breakpoints, and task steps.
3. Separate observed facts from assumptions. Ask for missing context only when it changes the recommendation materially.
4. State the intended attention order and the shortest successful user path before judging the design.

## Review in passes

### Pass 1: Task clarity

- Check whether users can recognize the page purpose, current state, next action, and expected result.
- Trace the primary task from entry through completion and recovery.
- Flag missing context, ambiguous labels, hidden prerequisites, and competing primary actions.

### Pass 2: Visual structure

- Check hierarchy, grouping, alignment, spacing, typography, color, and surface separation.
- Verify that visual emphasis follows user and business priority.
- Distinguish intentional asymmetry from accidental imbalance.

### Pass 3: Interaction quality

- Count decisions, inputs, navigation changes, waits, and repeated work.
- Check feedback, validation timing, error prevention, recovery, and destructive-action safeguards.
- Review keyboard, pointer, touch, zoom, reflow, and assistive-technology implications when relevant.

### Pass 4: States and resilience

- Inspect loading, empty, partial, success, validation, failure, offline, permission, and not-found states.
- Check narrow and wide layouts, long content, localization growth, and real data extremes.
- Identify where the design depends on color, hover, memory, or perfect input.

### Pass 5: Validation

- Treat aesthetic advice as a testable hypothesis, not a universal rule.
- Verify accessibility claims against the current applicable standard and platform guidance.
- Recommend real-user observation for uncertain workflows, high-risk tasks, and meaningful redesigns.

## Prioritize findings

Rank each finding by:

- User impact and affected task
- Frequency or reach
- Severity and recovery cost
- Confidence in the evidence
- Implementation effort and dependencies

Lead with a small set of high-impact findings. Do not bury blocked tasks beneath cosmetic polish.

## Route focused work

Invoke a focused skill when one concern needs deeper treatment:

- `$establish-visual-hierarchy`, `$organize-ui-spacing`, or `$compose-balanced-layouts`
- `$clarify-and-simplify-interfaces`, `$reduce-interaction-cost`, or `$design-usable-forms`
- `$design-accessible-color`, `$craft-readable-typography`, or `$create-depth-and-separation`
- `$use-visual-cues`, `$design-safe-destructive-actions`, `$design-mobile-touch-layouts`
- `$design-helpful-empty-error-states` or `$design-clear-navigation`

## Avoid these failures

- Do not redesign from personal taste alone.
- Do not prescribe isolated styling changes without naming the user problem.
- Do not assume fewer elements, clicks, or colors always produce a better outcome.
- Do not invent user research or accessibility conformance.
- Do not flatten every issue into the same priority.
- Do not expand scope beyond the artifact and task under review.

## Produce the result

Return:

1. A concise assessment of the interface and primary task.
2. Prioritized findings with evidence, user impact, and concrete changes.
3. Strengths worth preserving.
4. Accessibility or platform checks that require verification.
5. Open assumptions and a focused validation plan.
