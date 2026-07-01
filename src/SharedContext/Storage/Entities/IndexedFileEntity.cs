namespace Orchi.SharedContext.Storage.Entities;

public sealed class IndexedFileEntity
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public WorkspaceEntity Workspace { get; set; } = null!;

    public required string RelativePath { get; set; }

    public required string ContentHash { get; set; }

    public string? Language { get; set; }

    public int LineCount { get; set; }

    public string? Summary { get; set; }

    public DateTimeOffset IndexedAt { get; set; }
}
