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

    /// <summary>Branch checked out in this workspace (worktrees).</summary>
    public string? Branch { get; set; }

    /// <summary>Base branch the worktree was created from.</summary>
    public string? BaseBranch { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Project Project { get; set; } = null!;
}
