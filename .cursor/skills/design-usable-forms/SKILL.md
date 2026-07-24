---
name: design-usable-forms
description: Create or review forms by matching controls to input, reducing unnecessary fields, and providing clear labels, guidance, validation, and completion feedback. Use for registration, checkout, search, settings, data entry, authentication, and multi-step form flows.
---

# Design Usable Forms

## Define the data task

1. Identify why each value is needed, when it is needed, and whether it is already known.
2. Remove optional or derivable fields unless their value justifies the effort.
3. Group remaining inputs by user intent and natural completion order.
4. Choose a single-page or multi-step flow based on task structure, not an arbitrary field count.

## Choose controls

- Match the control to the data type, number of choices, comparison need, and input device.
- Use visible labels that remain available after entry.
- Provide examples or helper text only when they prevent likely errors.
- Use appropriate input types, autocomplete tokens, keyboards, masks, and formatting tolerance.
- Support paste and autofill, including for codes and credentials, unless a documented security reason forbids it.
- Preserve a coherent keyboard and reading order.

## Validate helpfully

- Prevent impossible values through control constraints where practical.
- Validate at a useful moment without interrupting unfinished input.
- Place a specific error next to the affected field and summarize errors when the form is long or submitted.
- Explain what happened and how to correct it; preserve the user's valid entries.
- Do not signal errors through color alone.
- Confirm successful submission and what happens next.

## Handle complexity

- Reveal conditional fields only when triggered, without surprising focus changes.
- Show progress and allow safe back navigation in multi-step flows.
- Save drafts or preserve state for costly forms.
- Explain why sensitive or unusual information is requested.
- Design retry behavior for server errors, duplicates, expiration, and interrupted sessions.

## Avoid these failures

- Do not use placeholder text as the only label.
- Do not split inputs into segments unless paste, autofill, editing, and assistive technology remain reliable.
- Do not require confirmation fields by habit; use them only when they reduce a demonstrated risk.
- Do not clear the form after an error.
- Do not mark optional fields inconsistently.

## Produce the result

Return:

1. A field inventory with keep, remove, derive, or defer decisions.
2. Control and input-mode choices.
3. Label, guidance, validation, and error behavior.
4. Keyboard, autofill, localization, and accessibility checks.
5. The submission, retry, and success flow.
