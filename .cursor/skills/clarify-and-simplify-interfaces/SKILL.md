---
name: clarify-and-simplify-interfaces
description: Clarify an interface by removing avoidable complexity while preserving necessary context, choices, and actions. Use when a screen feels cluttered, confusing, jargon-heavy, overfeatured, or difficult to understand despite being technically complete.
---

# Clarify and Simplify Interfaces

## Find the essential task

1. State what users are trying to accomplish and what they need to decide.
2. Inventory content, controls, decoration, repeated explanations, and hidden dependencies.
3. Mark each element as essential now, useful later, conditional, redundant, or irrelevant.
4. Identify complexity caused by the product model rather than the presentation.

## Simplify deliberately

- Remove duplicates and low-value decoration.
- Rewrite labels and instructions in recognizable user language.
- Show only information needed for the current decision while keeping important consequences visible.
- Group related choices and reveal advanced or conditional controls at the moment they become relevant.
- Replace avoidable choices with safe defaults where user intent is predictable.
- Preserve escape routes, comparison context, and access to less-common tasks.

## Check whether clarity improved

- Ask whether users can explain the page purpose, current state, next step, and outcome.
- Compare recognition demands with recall demands.
- Test realistic first-time, repeat, and edge-case tasks.
- Review empty, loading, error, permission, and destructive states.
- Confirm that progressive disclosure remains discoverable and keyboard accessible.

## Decide what not to simplify

- Keep information required for safety, informed consent, legal understanding, or irreversible decisions.
- Keep meaningful distinctions between options.
- Keep status and provenance when users must trust the result.
- Keep controls that serve important but less-frequent workflows when no safe alternative exists.

## Avoid these failures

- Do not equate minimal appearance with simple use.
- Do not hide complexity behind unlabeled icons or vague menus.
- Do not remove context merely to shorten the page.
- Do not merge distinct actions into an ambiguous control.
- Do not use jargon as a substitute for precise explanations.

## Produce the result

Return:

1. The essential task and decision.
2. A keep, remove, combine, defer, or rewrite inventory.
3. A simplified content and control structure.
4. Risks introduced by hiding or removing information.
5. Validation questions for representative users.
