---
name: design-safe-destructive-actions
description: Create or review destructive and irreversible actions with clear labels, appropriate friction, safe placement, confirmations, undo, and recovery. Use for delete, remove, reset, revoke, overwrite, cancel, archive, or high-impact bulk operations.
---

# Design Safe Destructive Actions

## Assess the risk

1. Identify the exact object, scope, consequence, reversibility, and affected people.
2. Estimate likelihood of accidental activation and cost of recovery.
3. Distinguish reversible removal, soft deletion, archival, and permanent destruction.
4. Apply friction in proportion to risk.

## Design the action

- Use a specific verb and object, such as "Delete 12 invoices," rather than "Yes" or "Confirm."
- Keep destructive styling reserved for genuinely destructive choices.
- Separate the action from frequent or benign controls without hiding it completely.
- Follow established platform and product conventions for button order; do not assume one universal left or right placement.
- Show scope and consequences before commitment.
- Disable the action only with a clear explanation when requirements are unmet.

## Choose the safeguard

- Prefer undo, trash, version history, or a recovery window when feasible.
- Use a confirmation dialog when the action is hard to reverse, costly, unusual, or easy to trigger accidentally.
- Require stronger confirmation, such as typing an identifier, only for exceptional risk.
- Avoid repetitive confirmations that train users to approve reflexively.
- Make cancel or escape obvious and safe.

## Handle completion and failure

- Confirm what changed and whether recovery remains possible.
- Keep selection and context when a bulk action partially fails.
- Report affected and unaffected items precisely.
- Prevent duplicate execution during slow operations.
- Log or expose audit information when accountability matters.

## Avoid these failures

- Do not use vague confirmation copy.
- Do not make the destructive option the default focused action.
- Do not hide consequences behind a tooltip or policy link.
- Do not rely on red alone to communicate danger.
- Do not promise undo unless restoration is reliable.

## Produce the result

Provide:

1. A risk and reversibility assessment.
2. The action label, placement, and visual treatment.
3. The chosen safeguard and why it is proportional.
4. Exact confirmation, completion, failure, and recovery behavior.
5. Keyboard, focus, and accessibility requirements.
