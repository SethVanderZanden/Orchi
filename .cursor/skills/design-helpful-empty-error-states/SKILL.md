---
name: design-helpful-empty-error-states
description: Create or review actionable empty, validation, failure, offline, permission, and not-found states. Use when an interface reaches a dead end, lacks recovery guidance, shows generic errors, or needs first-use and zero-data experiences.
---

# Design Helpful Empty and Error States

## Classify the state

1. Identify whether the state is expected empty data, filtered zero results, first use, validation, transient failure, permanent failure, offline, permission denial, or not found.
2. State what the user was trying to do and what remains possible.
3. Preserve relevant context, input, and navigation.
4. Avoid presenting an expected empty state as a system error.

## Compose the message

- Use a concise heading that names the situation in user language.
- Explain the cause only when known and useful.
- Give the most likely recovery or next step.
- Offer secondary options such as changing filters, returning, contacting support, or viewing status when relevant.
- Include a reference or diagnostic detail only when it helps support or retry.
- Use imagery or icons as reinforcement, not as the only explanation.

## Design recovery

- Provide retry for transient failures without duplicating work.
- Preserve entered data and selections.
- Distinguish field-level correction from form-level or system-level failure.
- For empty states, teach the value and offer a relevant first action.
- For not-found states, provide orientation and useful destinations.
- For permission states, explain what access is needed and who can grant it when known.

## Verify resilience

- Check focus placement, announcements, reading order, and keyboard operation.
- Do not rely on color alone for severity or status.
- Test repeated failure, partial data, slow retry, expired sessions, and offline transitions.
- Avoid exposing sensitive implementation details.
- Confirm that recovery actions remain available at narrow widths and high zoom.

## Avoid these failures

- Do not use "Something went wrong" when a more specific safe message is available.
- Do not blame the user.
- Do not erase work after an error.
- Do not show a cheerful empty-state promotion during an outage.
- Do not offer retry for a permanent or authorization failure without explanation.

## Produce the result

Provide:

1. The state classification and user goal.
2. Exact heading, explanation, and actions.
3. Preservation, retry, and fallback behavior.
4. Accessibility and privacy requirements.
5. Edge cases and telemetry needed to validate recovery.
