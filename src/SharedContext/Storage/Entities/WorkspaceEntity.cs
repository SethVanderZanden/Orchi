namespace Orchi.SharedContext.Storage.Entities;

public sealed class WorkspaceEntity
{
    public Guid Id { get; set; }

    public required string NormalizedPath { get; set; }

    public DateTimeOffset? LastIndexedAt { get; set; }

    public string? GitBranch { get; set; }

    public string? GitHead { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<IndexedFileEntity> Files { get; set; } = [];

    public ICollection<SymbolEntity> Symbols { get; set; } = [];

    public ICollection<TaskSummaryEntity> TaskSummaries { get; set; } = [];
}
