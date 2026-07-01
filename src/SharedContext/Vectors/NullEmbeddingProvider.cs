namespace Orchi.SharedContext.Vectors;

internal sealed class NullEmbeddingProvider : IEmbeddingProvider
{
    public int Dimensions => 0;

    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken) =>
        Task.FromResult(Array.Empty<float>());
}
