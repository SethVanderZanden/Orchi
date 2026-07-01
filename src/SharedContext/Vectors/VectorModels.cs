namespace Orchi.SharedContext.Vectors;

public sealed record VectorDocument(
    string Id,
    string Kind,
    string Text,
    string? SourcePath);

public sealed record VectorSearchQuery(
    string WorkspacePath,
    string Query,
    int TopK = 8);

public sealed record ScoredChunk(
    string Kind,
    string Title,
    string Content,
    string? SourcePath,
    double Score);

public interface IEmbeddingProvider
{
    int Dimensions { get; }

    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken);
}

public interface IVectorStore
{
    Task UpsertAsync(string workspacePath, IReadOnlyList<VectorDocument> documents, CancellationToken cancellationToken);

    Task<IReadOnlyList<ScoredChunk>> SearchAsync(VectorSearchQuery query, CancellationToken cancellationToken);
}
