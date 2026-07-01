# Event Bus

## Dummy section (start here)

The event bus is the office **PA system** — when something happens (file changed, turn completed), interested workers hear it without everyone polling each other.

| Analogy | Orchi |
|---------|-------|
| PA announcement | `IWorkspaceEventBus.PublishAsync` |
| Listeners | `WorkspaceIndexWorker`, future subscribers |
| Bulletin types | `WorkspaceEventKind` enum |

---

## Status

**done** — see [PROGRESS.md](PROGRESS.md)

## Interface

```csharp
public interface IWorkspaceEventBus
{
    ValueTask PublishAsync(WorkspaceEvent workspaceEvent, CancellationToken cancellationToken);
    ChannelReader<WorkspaceEvent> SubscribeAll();
}
```

In-process `Channel<WorkspaceEvent>` — no external broker in Phase 2.

## Events

| Event | Publisher | Subscriber |
|-------|-----------|------------|
| `TurnCompleted` | `AgentSessionManager` | `WorkspaceIndexWorker` |
| `ModeChanged` | `UpdateModeAsync` | Prompt context |
| `ChatActivity` | Child chat activity | Goal check-in + bus |
| `WorkspaceIndexed` | Session create | Index worker |

## Worker

`WorkspaceIndexWorker` debounces index requests (10s) and calls `IProjectIndexer` when workspace is stale.

Implementation: [`src/SharedContext/Events/`](../../src/SharedContext/Events/)
