namespace Orchi.SharedContext.Storage.Entities;

public sealed class TaskSummaryEntity
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public WorkspaceEntity Workspace { get; set; } = null!;

    public Guid ChatId { get; set; }

    public required string Summary { get; set; }

    public string Status { get; set; } = "active";

    public DateTimeOffset UpdatedAt { get; set; }
}
