---
name: orchi-durable-docs
description: >-
  Keep docs/ limited to durable how-Orchi-works guides. Use when creating or editing
  docs under docs/, deciding whether content belongs in docs vs a PR/issue, or when
  tempted to add roadmaps, phased plans, non-goals, or future-work dumps to documentation.
---

# Durable documentation scope

`docs/` is for **what Orchi is and how it works today** — not a scratchpad for agent or design work-in-progress.

Dummy-section format lives in the **orchi-documentation** skill. This skill only covers **what** may be committed under `docs/`.

## Do not put in `docs/`

| Ban | Examples |
|-----|----------|
| Roadmaps / phased rollouts | “Phase 0… Phase 3”, “done in tree”, “tracked as follow-up” |
| Temp plans / design dumps | Long “Goals / Non-goals / Success criteria” for unfinished work |
| Future feature speculation | “Optional UI later”, unverified guesses framed as product status |
| Agent scratch notes | Post-review checklists, PR narratives, verification TODOs |

Put that material in the **PR description**, issue tracker, or chat — not under `docs/`.

## Do put in `docs/`

- How a shipped feature/pattern works (contracts, file paths, config keys)
- Short “how to extend” steps that match **current** code
- Operator facts: install prerequisites, resolution order, spawn rules

## Prefer merge over multiply

Before adding `docs/patterns/{new}.md`, ask: can this be a **section** in an existing guide? Prefer one short durable guide over “suite + extensibility plan + roadmap”.

## Checklist

- [ ] Content describes current behavior (not a plan)
- [ ] No Phase/roadmap/non-goals/success-criteria blocks for unfinished work
- [ ] Not duplicating the same essay across agent + pattern docs
- [ ] If deleting a doc, index links updated
