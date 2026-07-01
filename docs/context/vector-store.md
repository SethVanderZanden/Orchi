# Vector Store

## Dummy section (start here)

Semantic search is like asking the librarian **"find me anything about authentication"** instead of knowing the exact shelf. Phase 2 starts with keyword search; Phase 3 adds embeddings.

| Analogy | Orchi |
|---------|-------|
| Ask the librarian | `IVectorStore.SearchAsync` |
| Card catalog lookup (today) | `KeywordVectorStore` → `IContextStore.QueryAsync` |
| Future: meaning-based index | `IEmbeddingProvider` + sqlite-vec / LanceDB |

---

## Status

**done** (keyword stub) — embeddings **not started** — see [PROGRESS.md](PROGRESS.md)

## Interfaces

```csharp
public interface IVectorStore
{
    Task UpsertAsync(string workspacePath, IReadOnlyList<VectorDocument> docs, CancellationToken ct);
    Task<IReadOnlyList<ScoredChunk>> SearchAsync(VectorSearchQuery query, CancellationToken ct);
}

public interface IEmbeddingProvider
{
    int Dimensions { get; }
    Task<float[]> EmbedAsync(string text, CancellationToken ct);
}
```

## Current implementation

- `KeywordVectorStore` — delegates to context store `Contains` queries on paths/summaries/symbol names.
- `NullEmbeddingProvider` — placeholder until Phase 3.

## Phase 3 plan

1. Choose embedding provider behind `IEmbeddingProvider`.
2. Embed file summaries, symbols, decisions.
3. Hybrid search: keyword pre-filter + vector rerank.
