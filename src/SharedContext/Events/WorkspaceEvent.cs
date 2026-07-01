namespace Orchi.SharedContext.Events;

public enum WorkspaceEventKind
{
    TaskCreated,
    TaskAssigned,
    TaskCompleted,
    FileChanged,
    SummaryUpdated,
    BuildFinished,
    TestsPassed,
    TestsFailed,
    ReviewRequired,
    MergeCompleted,
    ModeChanged,
    ChatActivity,
    TurnCompleted,
    WorkspaceIndexed
}

public sealed record WorkspaceEvent(
    WorkspaceEventKind Kind,
    string WorkspacePath,
    DateTimeOffset OccurredAt,
    Guid? ChatId = null,
    string? Payload = null);
