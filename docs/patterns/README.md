# Software Patterns

Orchi tracks the design patterns and architectural styles used across the codebase. Each guide explains what the pattern is, where it appears in Orchi, and where to read more.

## Design patterns

| Pattern | Where in Orchi | Guide |
|---------|----------------|-------|
| **Decorator** | CQRS behaviours (`ValidationBehaviour`, `LoggingBehaviour`, `PerformanceBehaviour`) wired via Scrutor | [Decorator](decorator.md) — start with the [Dummy section](decorator.md#dummy-section-start-here) |
| **Result object** | `Result` / `Result<T>` returned from handlers instead of throwing for expected failures | [Result object](result-object.md) |
| **Options** | `PerformanceOptions` and other config-bound settings | [Options](options.md) |
| **Dependency injection** | Constructor injection throughout; handlers and behaviours resolved from the container | [Dependency injection](dependency-injection.md) |

## Architectural styles

These are broader structural choices documented under [architecture](../architecture/README.md):

| Style | Summary | Guide |
|-------|---------|-------|
| **Vertical Slice Architecture** | One use case per slice — handler, validator, endpoint together | [VSA](../architecture/vertical-slice-architecture.md) |
| **Screaming Architecture** | Folder structure reflects domain use cases, not framework layers | [Screaming Architecture](../architecture/screaming-architecture.md) |
| **CQRS** | Separate query and command paths with dedicated handler interfaces | [CQRS Pipeline](../architecture/cqrs-pipeline.md) |

## Adding a new pattern

When introducing a pattern to Orchi:

1. Add or extend an implementation in `src/API/`
2. Create `docs/patterns/{pattern-name}.md` with a **[Dummy section](DOCUMENTATION.md)** first, then definition, Orchi usage, examples, and FAQ
3. Add a row to the table above
4. Cross-link from relevant architecture or testing docs

See [Documentation convention](DOCUMENTATION.md) for the Dummy section template.

## Related docs

- [Architecture overview](../architecture/README.md)
- [Adding a Feature](../architecture/adding-a-feature.md)
- [Unit Testing](../testing/unit-testing.md)
