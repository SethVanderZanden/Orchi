namespace Orchi.SharedContext.Storage.Entities;

public sealed class SymbolEntity
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public WorkspaceEntity Workspace { get; set; } = null!;

    public required string RelativePath { get; set; }

    public required string Name { get; set; }

    public required string Kind { get; set; }

    public int StartLine { get; set; }

    public int EndLine { get; set; }

    public string? ParentSymbol { get; set; }
}
