namespace Orchi.SharedContext.Indexing;

public sealed record IndexOptions(bool FullRebuild = false, int? MaxFiles = null);

public sealed record IndexResult(
    int FilesScanned,
    int FilesUpdated,
    int SymbolsExtracted,
    string? GitBranch,
    string? GitHead);

public sealed record FileIndexEntry(
    string RelativePath,
    string ContentHash,
    string? Language,
    int LineCount,
    string? Summary,
    IReadOnlyList<SymbolIndexEntry> Symbols);

public sealed record SymbolIndexEntry(
    string Name,
    string Kind,
    int StartLine,
    int EndLine,
    string? ParentSymbol);
