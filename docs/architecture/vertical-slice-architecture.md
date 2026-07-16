# Vertical Slice Architecture

## Dummy section (start here)

Traditional web apps split one feature across many folders — controller here, service there, validator somewhere else. Finding "how do I create a chat?" means opening five files in five places.

**Vertical Slice Architecture (VSA)** stacks everything for one use case in one column — like a **single recipe card** that lists ingredients, steps, and how to plate the dish.

```
Layered (horizontal)                    Vertical slice (one card)

Controllers/CreateChatController.cs     Features/Chats/CreateChat/CreateChat.cs
Services/ChatService.cs                      ├── Command + Handler
Validators/CreateChatValidator.cs            ├── Validator
Models/CreateChatRequest.cs                └── Endpoint
```

**The aha:** one feature = one place to read and change — fewer hops, clearer ownership.

Everything below is the same idea with Orchi's slice conventions.

---

Adapted from [Milan Jovanovic's VSA guides](https://www.milanjovanovic.tech/blog/vertical-slice-architecture-dotnet).

## What is Vertical Slice Architecture?

Traditional layered architecture scatters one feature across many folders:

```
Controllers/ProjectController.cs
Services/ProjectService.cs
Models/ProjectSummary.cs
Validators/ListProjectsValidator.cs
```

Vertical Slice Architecture groups everything for one use case in one place:

```
Features/Projects/ListProjects/ListProjects.cs
```

Each slice is self-contained: request/query, response, handler, validator, and endpoint mapping.

## Why VSA for Orchi?

| Benefit | How it helps |
|---------|--------------|
| **Cohesion** | All code for a feature lives together |
| **Reduced navigation** | No hunting across Controllers/Services/Repositories |
| **Independent evolution** | Simple features stay simple; complex ones can grow |
| **Easier testing** | Handler is a focused unit with clear inputs/outputs |
| **Aligned with business** | Structure matches use cases, not framework layers |

## Orchi's slice structure

Each feature slice is a static class in `Features/{Domain}/{UseCase}/`:

**Query example** — [`ListProjects.cs`](../../src/API/Features/Projects/ListProjects/ListProjects.cs):

```csharp
public static class ListProjects
{
    public sealed record Query : IQuery<IReadOnlyList<ProjectSummaryResponse>>;

    internal sealed class Handler(IProjectStore projectStore)
        : IQueryHandler<Query, IReadOnlyList<ProjectSummaryResponse>> { ... }

    public sealed class Endpoint : IEndpoint { ... }
}
```

**Command example** — [`CreateChat.cs`](../../src/API/Features/Chats/CreateChat/CreateChat.cs):

```csharp
public static class CreateChat
{
    public sealed record Command(...) : ICommand<CreateChatResponse>;

    internal sealed class Handler(AgentSessionManager sessionManager)
        : ICommandHandler<Command, CreateChatResponse> { ... }

    public sealed class Validator : AbstractValidator<Command> { ... }
    public sealed class Endpoint : IEndpoint { ... }
}
```

## One file vs multiple files

Orchi defaults to **one file per slice** for maximum locality. Split into multiple files when a slice grows beyond ~300–400 lines or has complex validation with many rules.

Both approaches are valid — locality matters more than file count.

## Queries vs commands

- **Query** (`IQuery<TResponse>`) — read-only, no side effects (e.g. `ListProjects`, `GetChat`)
- **Command** (`ICommand` or `ICommand<TResponse>`) — writes, state changes (e.g. `CreateChat`, `CloseChat`)

For commands that only need success/failure, use `ICommand` with `ICommandHandler<TCommand>` — see [`CloseChat.cs`](../../src/API/Features/Chats/CloseChat/CloseChat.cs).

## Further reading

- [Vertical Slice Architecture Is Easier Than You Think](https://www.milanjovanovic.tech/blog/vertical-slice-architecture-is-easier-than-you-think)
- [Structuring Vertical Slices](https://www.milanjovanovic.tech/blog/vertical-slice-architecture-structuring-vertical-slices)
- [Screaming Architecture](screaming-architecture.md) — how Orchi names its feature folders
- [CQRS Pipeline](cqrs-pipeline.md) — how handlers are invoked and wrapped
