using Orchi.SharedContext.Storage;

namespace Orchi.SharedContext.Vectors;

internal sealed class KeywordVectorStore(IContextStore contextStore) : IVectorStore
{
    public Task UpsertAsync(string workspacePath, IReadOnlyList<VectorDocument> documents, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public async Task<IReadOnlyList<ScoredChunk>> SearchAsync(VectorSearchQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<ContextChunk> chunks = await contextStore.QueryAsync(
            new ContextQuery(query.WorkspacePath, query.Query, query.TopK),
            cancellationToken);

        return chunks
            .Select(chunk => new ScoredChunk(chunk.Kind, chunk.Title, chunk.Content, chunk.SourcePath, chunk.Score))
            .ToList();
    }
}
