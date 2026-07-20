---
name: orchi-documentation
description: >-
  Write and update Orchi documentation with a Dummy section first — plain-language
  analogy before technical detail. Use when creating or editing docs under docs/,
  adding pattern guides, architecture write-ups, README sections, or when the user
  asks for documentation.
---

# Orchi Documentation

Seth connects easier with simplicity first. Every substantive doc must open with a **Dummy section** before technical content.

## Non-negotiable: Dummy section

Place this **immediately after the title** (before overview, architecture, or code):

```markdown
# [Topic Title]

## Dummy section (start here)

[Plain-language explanation anyone can skim and understand]

---

[Technical content begins here]
```

### Dummy section requirements

1. **Assume zero prior knowledge** — no jargon without a one-line plain-English definition
2. **Use a concrete analogy** — pizza wrappers, mail forwarding, layers of an onion, etc.
3. **Show the idea visually** — simple ASCII diagram or short table mapping analogy → Orchi
4. **State the "aha" in one sentence** — what problem this solves in human terms
5. **Bridge to Orchi** — small table or bullet list: "In Orchi, X = Y"
6. **Signpost the rest** — end with something like: "Everything below is the same idea with C#/DI/code."

Keep the Dummy section **short** (roughly 15–40 lines). It is a ramp, not the full doc.

### Canonical example

See [docs/patterns/decorator.md](../../../docs/patterns/decorator.md#dummy-section-start-here) — pizza wrappers → CQRS behaviours.

## Doc workflow

When writing or updating Orchi docs:

1. Add or refresh the **Dummy section** first
2. Write the technical body (diagrams, code, tables, FAQ)
3. Cross-link related docs; avoid duplicating full explanations in two places
4. Link from index pages (`docs/patterns/README.md`, `docs/architecture/README.md`) to `#dummy-section-start-here` when helpful

For what belongs in `docs/` vs PR/issue scratchpads, see the **orchi-durable-docs** skill.

## Where docs live

| Area | Path | Purpose |
|------|------|---------|
| Architecture | `docs/architecture/` | VSA, CQRS, feature guides |
| Agents | `docs/agents/` | Agent adapters, CLI, prompt composition |
| Patterns | `docs/patterns/` | Design patterns with Dummy + deep dive |
| Testing | `docs/testing/` | Test patterns |

## Tone

- Friendly and direct — write for a smart person who prefers simple entry points
- Complete sentences; no telegraphic shorthand in Dummy sections
- Technical sections can be denser, but still scannable (headings, tables, short paragraphs)

## Checklist before finishing

- [ ] Dummy section exists directly under the title
- [ ] Analogy is concrete (not abstract "wrapper pattern" with no picture)
- [ ] Orchi mapping is explicit
- [ ] Horizontal rule `---` separates Dummy from technical content
- [ ] Index/README links updated if this is a new doc
