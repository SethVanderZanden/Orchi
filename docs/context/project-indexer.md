# Project Indexer

## Dummy section (start here)

The indexer is like a **librarian** who walks the shelves, notes what changed since last visit, and updates the card catalog — without re-reading every book every time.

| Analogy | Orchi |
|---------|-------|
| Walking the shelves | `WorkspaceFileDiscovery.EnumerateSourceFiles` |
| "This copy changed" | SHA-256 per file vs stored hash |
| Card catalog entry | `IndexedFile` + `Symbol` rows in context store |
| Branch sticker | Git branch/head via `git` CLI |

---

## Status

**done** — see [PROGRESS.md](PROGRESS.md)

## Interface

```csharp
public interface IProjectIndexer
{
    Task<IndexResult> IndexAsync(string workspacePath, IndexOptions options, CancellationToken ct);
    Task<FileIndexEntry?> GetFileAsync(string workspacePath, string relativePath, CancellationToken ct);
    bool IsStale(string workspacePath, DateTimeOffset? lastIndexedAt);
}
```

Implementation: [`src/SharedContext/Indexing/ProjectIndexer.cs`](../../src/SharedContext/Indexing/ProjectIndexer.cs)

## Behavior

- Walks workspace; excludes `.git`, `node_modules`, `bin`, `obj`, etc.
- Incremental: only re-parses files whose hash changed.
- Extracts C#/TS symbols via regex heuristics (`SymbolExtractor`).
- Reads git branch/head when `git` is available.
- Builds one-line file summaries for retrieval.

## Triggers

- `WorkspaceIndexed` event on new chat session (debounced via `WorkspaceIndexWorker`).
- `TurnCompleted`, `FileChanged`, `TaskCompleted` events.
- Manual: `POST /workspaces/index`.

## Tests

Indexer behavior covered indirectly via integration; add dedicated tests under `tests/Orchi.Api.Tests/Infrastructure/SharedContext/` as needed.
