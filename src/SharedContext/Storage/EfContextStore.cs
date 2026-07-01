using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orchi.SharedContext.Storage.Entities;

namespace Orchi.SharedContext.Storage;

internal sealed class EfContextStore(
    IDbContextFactory<SharedContextDbContext> dbFactory,
    IOptions<SharedContextOptions> options) : IContextStore
{
    public async Task<WorkspaceContext> GetOrCreateWorkspaceAsync(string workspacePath, CancellationToken cancellationToken)
    {
        string normalized = NormalizePath(workspacePath);
        await using SharedContextDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

        WorkspaceEntity? entity = await db.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(workspace => workspace.NormalizedPath == normalized, cancellationToken);

        if (entity is not null)
        {
            return await MapWorkspaceAsync(db, entity, cancellationToken);
        }

        var now = DateTimeOffset.UtcNow;
        entity = new WorkspaceEntity
        {
            Id = Guid.NewGuid(),
            NormalizedPath = normalized,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Workspaces.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return await MapWorkspaceAsync(db, entity, cancellationToken);
    }

    public async Task<WorkspaceContext?> GetWorkspaceAsync(string workspacePath, CancellationToken cancellationToken)
    {
        string normalized = NormalizePath(workspacePath);
        await using SharedContextDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

        WorkspaceEntity? entity = await db.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(workspace => workspace.NormalizedPath == normalized, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return await MapWorkspaceAsync(db, entity, cancellationToken);
    }

    public async Task<IReadOnlyList<ContextChunk>> QueryAsync(ContextQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query.SearchText))
        {
            return [];
        }

        string normalized = NormalizePath(query.WorkspacePath);
        await using SharedContextDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

        WorkspaceEntity? workspace = await db.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.NormalizedPath == normalized, cancellationToken);

        if (workspace is null)
        {
            return [];
        }

        string term = query.SearchText.Trim();
        int topK = Math.Min(query.TopK, options.Value.RetrievalTopK);

        List<ContextChunk> fileChunks = await db.IndexedFiles
            .AsNoTracking()
            .Where(file => file.WorkspaceId == workspace.Id &&
                           (file.RelativePath.Contains(term) ||
                            (file.Summary != null && file.Summary.Contains(term))))
            .OrderByDescending(file => file.IndexedAt)
            .Take(topK)
            .Select(file => new ContextChunk(
                "file",
                file.RelativePath,
                file.Summary ?? file.RelativePath,
                file.RelativePath))
            .ToListAsync(cancellationToken);

        if (fileChunks.Count >= topK)
        {
            return fileChunks;
        }

        int remaining = topK - fileChunks.Count;
        List<ContextChunk> symbolChunks = await db.Symbols
            .AsNoTracking()
            .Where(symbol => symbol.WorkspaceId == workspace.Id &&
                             (symbol.Name.Contains(term) || symbol.RelativePath.Contains(term)))
            .Take(remaining)
            .Select(symbol => new ContextChunk(
                "symbol",
                $"{symbol.Kind} {symbol.Name}",
                $"{symbol.Kind} {symbol.Name} in {symbol.RelativePath}:{symbol.StartLine}",
                symbol.RelativePath))
            .ToListAsync(cancellationToken);

        fileChunks.AddRange(symbolChunks);
        return fileChunks;
    }

    public async Task UpsertAsync(ContextUpsert upsert, CancellationToken cancellationToken)
    {
        WorkspaceContext workspace = await GetOrCreateWorkspaceAsync(upsert.WorkspacePath, cancellationToken);
        await using SharedContextDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

        WorkspaceEntity entity = await db.Workspaces
            .FirstAsync(w => w.Id == workspace.WorkspaceId, cancellationToken);

        if (upsert.GitBranch is not null)
        {
            entity.GitBranch = upsert.GitBranch;
        }

        if (upsert.GitHead is not null)
        {
            entity.GitHead = upsert.GitHead;
        }

        if (upsert.LastIndexedAt is not null)
        {
            entity.LastIndexedAt = upsert.LastIndexedAt;
        }

        entity.UpdatedAt = DateTimeOffset.UtcNow;

        if (upsert.Files is { Count: > 0 })
        {
            await UpsertFilesAsync(db, entity.Id, upsert.Files, cancellationToken);
        }

        if (upsert.Symbols is { Count: > 0 })
        {
            await UpsertSymbolsAsync(db, entity.Id, upsert.Symbols, cancellationToken);
        }

        if (upsert.TaskSummary is not null)
        {
            await UpsertTaskSummaryAsync(db, entity.Id, upsert.TaskSummary, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetSessionSummaryAsync(string workspacePath, Guid chatId, CancellationToken cancellationToken)
    {
        string normalized = NormalizePath(workspacePath);
        await using SharedContextDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

        return await db.TaskSummaries
            .AsNoTracking()
            .Where(summary => summary.ChatId == chatId &&
                              summary.Workspace.NormalizedPath == normalized)
            .Select(summary => summary.Summary)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertSessionSummaryAsync(
        string workspacePath,
        Guid chatId,
        string summary,
        string status,
        CancellationToken cancellationToken)
    {
        await UpsertAsync(
            new ContextUpsert(
                workspacePath,
                TaskSummary: new TaskSummaryUpsert(chatId, summary, status)),
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetFileHashesAsync(
        string workspacePath,
        CancellationToken cancellationToken)
    {
        string normalized = NormalizePath(workspacePath);
        await using SharedContextDbContext db = await dbFactory.CreateDbContextAsync(cancellationToken);

        WorkspaceEntity? workspace = await db.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.NormalizedPath == normalized, cancellationToken);

        if (workspace is null)
        {
            return new Dictionary<string, string>();
        }

        return await db.IndexedFiles
            .AsNoTracking()
            .Where(file => file.WorkspaceId == workspace.Id)
            .ToDictionaryAsync(file => file.RelativePath, file => file.ContentHash, cancellationToken);
    }

    private static async Task UpsertFilesAsync(
        SharedContextDbContext db,
        Guid workspaceId,
        IReadOnlyList<IndexedFileUpsert> files,
        CancellationToken cancellationToken)
    {
        var paths = files.Select(file => file.RelativePath).ToList();
        Dictionary<string, IndexedFileEntity> existing = await db.IndexedFiles
            .Where(file => file.WorkspaceId == workspaceId && paths.Contains(file.RelativePath))
            .ToDictionaryAsync(file => file.RelativePath, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (IndexedFileUpsert file in files)
        {
            if (existing.TryGetValue(file.RelativePath, out IndexedFileEntity? entity))
            {
                entity.ContentHash = file.ContentHash;
                entity.Language = file.Language;
                entity.LineCount = file.LineCount;
                entity.Summary = file.Summary;
                entity.IndexedAt = now;
                continue;
            }

            db.IndexedFiles.Add(new IndexedFileEntity
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                RelativePath = file.RelativePath,
                ContentHash = file.ContentHash,
                Language = file.Language,
                LineCount = file.LineCount,
                Summary = file.Summary,
                IndexedAt = now
            });
        }
    }

    private static async Task UpsertSymbolsAsync(
        SharedContextDbContext db,
        Guid workspaceId,
        IReadOnlyList<SymbolUpsert> symbols,
        CancellationToken cancellationToken)
    {
        var paths = symbols.Select(symbol => symbol.RelativePath).Distinct().ToList();
        List<SymbolEntity> existing = await db.Symbols
            .Where(symbol => symbol.WorkspaceId == workspaceId && paths.Contains(symbol.RelativePath))
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            db.Symbols.RemoveRange(existing);
        }

        foreach (SymbolUpsert symbol in symbols)
        {
            db.Symbols.Add(new SymbolEntity
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                RelativePath = symbol.RelativePath,
                Name = symbol.Name,
                Kind = symbol.Kind,
                StartLine = symbol.StartLine,
                EndLine = symbol.EndLine,
                ParentSymbol = symbol.ParentSymbol
            });
        }
    }

    private static async Task UpsertTaskSummaryAsync(
        SharedContextDbContext db,
        Guid workspaceId,
        TaskSummaryUpsert summary,
        CancellationToken cancellationToken)
    {
        TaskSummaryEntity? entity = await db.TaskSummaries
            .FirstOrDefaultAsync(
                row => row.WorkspaceId == workspaceId && row.ChatId == summary.ChatId,
                cancellationToken);

        var now = DateTimeOffset.UtcNow;
        if (entity is null)
        {
            db.TaskSummaries.Add(new TaskSummaryEntity
            {
                Id = Guid.NewGuid(),
                WorkspaceId = workspaceId,
                ChatId = summary.ChatId,
                Summary = summary.Summary,
                Status = summary.Status,
                UpdatedAt = now
            });
            return;
        }

        entity.Summary = summary.Summary;
        entity.Status = summary.Status;
        entity.UpdatedAt = now;
    }

    private static async Task<WorkspaceContext> MapWorkspaceAsync(
        SharedContextDbContext db,
        WorkspaceEntity entity,
        CancellationToken cancellationToken)
    {
        int fileCount = await db.IndexedFiles.CountAsync(file => file.WorkspaceId == entity.Id, cancellationToken);
        int symbolCount = await db.Symbols.CountAsync(symbol => symbol.WorkspaceId == entity.Id, cancellationToken);

        return new WorkspaceContext(
            entity.Id,
            entity.NormalizedPath,
            entity.LastIndexedAt,
            entity.GitBranch,
            entity.GitHead,
            fileCount,
            symbolCount);
    }

    internal static string NormalizePath(string workspacePath) =>
        WorkspacePathNormalizer.Normalize(workspacePath);
}
