namespace Orchi.SharedContext.Indexing;

public interface IProjectIndexer
{
    Task<IndexResult> IndexAsync(string workspacePath, IndexOptions options, CancellationToken cancellationToken);

    Task<FileIndexEntry?> GetFileAsync(string workspacePath, string relativePath, CancellationToken cancellationToken);

    bool IsStale(string workspacePath, DateTimeOffset? lastIndexedAt);
}
