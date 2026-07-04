namespace Orchi.Api.Entities;

public class Workspace
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public required string Path { get; set; }

    /// <summary>Normalized path for deduplication; matches desktop normalizeWorkspacePath rules.</summary>
    public required string NormalizedPath { get; set; }

    public required string Name { get; set; }

    public bool IsDefault { get; set; }

    public WorkspaceKind Kind { get; set; } = WorkspaceKind.Primary;

    public DateTimeOffset CreatedAt { get; set; }

    public Project Project { get; set; } = null!;
}
