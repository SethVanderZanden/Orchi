namespace Orchi.SharedContext.Storage;

public interface IContextStore
{
    Task<WorkspaceContext> GetOrCreateWorkspaceAsync(string workspacePath, CancellationToken cancellationToken);

    Task<WorkspaceContext?> GetWorkspaceAsync(string workspacePath, CancellationToken cancellationToken);

    Task<IReadOnlyList<ContextChunk>> QueryAsync(ContextQuery query, CancellationToken cancellationToken);

    Task UpsertAsync(ContextUpsert upsert, CancellationToken cancellationToken);

    Task<string?> GetSessionSummaryAsync(string workspacePath, Guid chatId, CancellationToken cancellationToken);

    Task UpsertSessionSummaryAsync(
        string workspacePath,
        Guid chatId,
        string summary,
        string status,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<string, string>> GetFileHashesAsync(string workspacePath, CancellationToken cancellationToken);
}
