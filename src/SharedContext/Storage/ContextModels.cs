namespace Orchi.SharedContext.Storage;

public sealed record WorkspaceContext(
    Guid WorkspaceId,
    string NormalizedPath,
    DateTimeOffset? LastIndexedAt,
    string? GitBranch,
    string? GitHead,
    int IndexedFileCount,
    int SymbolCount);

public sealed record ContextChunk(
    string Kind,
    string Title,
    string Content,
    string? SourcePath,
    double Score = 1.0);

public sealed record ContextQuery(
    string WorkspacePath,
    string? SearchText = null,
    int TopK = 8);

public sealed record ContextUpsert(
    string WorkspacePath,
    IReadOnlyList<IndexedFileUpsert>? Files = null,
    IReadOnlyList<SymbolUpsert>? Symbols = null,
    string? GitBranch = null,
    string? GitHead = null,
    DateTimeOffset? LastIndexedAt = null,
    TaskSummaryUpsert? TaskSummary = null);

public sealed record IndexedFileUpsert(
    string RelativePath,
    string ContentHash,
    string? Language,
    int LineCount,
    string? Summary);

public sealed record SymbolUpsert(
    string RelativePath,
    string Name,
    string Kind,
    int StartLine,
    int EndLine,
    string? ParentSymbol);

public sealed record TaskSummaryUpsert(Guid ChatId, string Summary, string Status);
