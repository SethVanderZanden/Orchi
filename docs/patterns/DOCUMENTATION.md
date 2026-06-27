# Documentation convention

Orchi docs use a **Dummy section** at the top of every substantive guide.

## Why

Technical docs are easier to absorb when there is a plain-language entry point first — an analogy, a diagram, and a clear "oh, okay" moment before code and DI wiring.

## Template

```markdown
# [Topic]

## Dummy section (start here)

[Everyday analogy. What problem does this solve in human terms?]

[Simple ASCII diagram or table]

**Orchi translation:** map analogy parts to real code/concepts.

That is the whole idea. Everything below is the same thing with more detail.

---

[Technical sections: What it is, How Orchi uses it, FAQ, etc.]
```

## Example

[Decorator pattern — Dummy section](decorator.md#dummy-section-start-here) (pizza wrappers → CQRS behaviours).

## For agents and contributors

- **Cursor rule:** `.cursor/rules/documentation-dummy-section.mdc` (applies when editing `docs/**/*.md`)
- **Skill:** `.cursor/skills/orchi-documentation/SKILL.md`
