using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orchi.SharedContext.Storage;

namespace Orchi.SharedContext.Indexing;

internal sealed class ProjectIndexer(
    IContextStore contextStore,
    IOptions<SharedContextOptions> options,
    ILogger<ProjectIndexer> logger) : IProjectIndexer
{
    public bool IsStale(string workspacePath, DateTimeOffset? lastIndexedAt)
    {
        if (lastIndexedAt is null)
        {
            return true;
        }

        return DateTimeOffset.UtcNow - lastIndexedAt.Value > options.Value.IndexStaleAfter;
    }

    public async Task<IndexResult> IndexAsync(string workspacePath, IndexOptions indexOptions, CancellationToken cancellationToken)
    {
        string fullPath = WorkspacePathNormalizer.Normalize(workspacePath);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Workspace path does not exist: {fullPath}");
        }

        int maxFiles = indexOptions.MaxFiles ?? options.Value.MaxFilesPerIndex;
        (string? gitBranch, string? gitHead) = GitWorkspaceMetadataReader.Read(fullPath);

        var fileUpserts = new List<IndexedFileUpsert>();
        var symbolUpserts = new List<SymbolUpsert>();
        int filesUpdated = 0;

        WorkspaceContext? existing = await contextStore.GetWorkspaceAsync(fullPath, cancellationToken);
        Dictionary<string, string>? existingHashes = null;

        if (!indexOptions.FullRebuild && existing is not null)
        {
            existingHashes = await LoadExistingHashesAsync(fullPath, cancellationToken);
        }

        foreach (string absolutePath in WorkspaceFileDiscovery.EnumerateSourceFiles(fullPath, maxFiles))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string relativePath = Path.GetRelativePath(fullPath, absolutePath).Replace('\\', '/');
            string hash = await ComputeHashAsync(absolutePath, cancellationToken);

            if (existingHashes is not null &&
                existingHashes.TryGetValue(relativePath, out string? existingHash) &&
                existingHash == hash)
            {
                continue;
            }

            string content;
            try
            {
                content = await File.ReadAllTextAsync(absolutePath, cancellationToken);
            }
            catch
            {
                continue;
            }

            string language = FileLanguageDetector.Detect(relativePath);
            int lineCount = content.Length == 0 ? 0 : content.Split('\n').Length;
            string summary = BuildSummary(relativePath, language, lineCount);

            fileUpserts.Add(new IndexedFileUpsert(relativePath, hash, language, lineCount, summary));
            filesUpdated++;

            foreach (SymbolIndexEntry symbol in SymbolExtractor.Extract(relativePath, content))
            {
                symbolUpserts.Add(new SymbolUpsert(
                    relativePath,
                    symbol.Name,
                    symbol.Kind,
                    symbol.StartLine,
                    symbol.EndLine,
                    symbol.ParentSymbol));
            }
        }

        if (fileUpserts.Count > 0 || gitBranch is not null || gitHead is not null)
        {
            await contextStore.UpsertAsync(
                new ContextUpsert(
                    fullPath,
                    Files: fileUpserts,
                    Symbols: symbolUpserts,
                    GitBranch: gitBranch,
                    GitHead: gitHead,
                    LastIndexedAt: DateTimeOffset.UtcNow),
                cancellationToken);
        }

        logger.LogInformation(
            "Indexed workspace {WorkspacePath}: scanned={Scanned}, updated={Updated}, symbols={Symbols}",
            fullPath,
            fileUpserts.Count,
            filesUpdated,
            symbolUpserts.Count);

        return new IndexResult(fileUpserts.Count, filesUpdated, symbolUpserts.Count, gitBranch, gitHead);
    }

    public async Task<FileIndexEntry?> GetFileAsync(string workspacePath, string relativePath, CancellationToken cancellationToken)
    {
        IReadOnlyList<ContextChunk> chunks = await contextStore.QueryAsync(
            new ContextQuery(WorkspacePathNormalizer.Normalize(workspacePath), relativePath, TopK: 1),
            cancellationToken);

        ContextChunk? chunk = chunks.FirstOrDefault(c => c.SourcePath == relativePath.Replace('\\', '/'));
        if (chunk is null)
        {
            return null;
        }

        return new FileIndexEntry(
            relativePath.Replace('\\', '/'),
            string.Empty,
            null,
            0,
            chunk.Content,
            []);
    }

    private async Task<Dictionary<string, string>> LoadExistingHashesAsync(
        string workspacePath,
        CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, string> hashes =
            await contextStore.GetFileHashesAsync(workspacePath, cancellationToken);

        return new Dictionary<string, string>(hashes, StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<string> ComputeHashAsync(string absolutePath, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(absolutePath);
        byte[] hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static string BuildSummary(string relativePath, string language, int lineCount) =>
        $"{language} file `{relativePath}` ({lineCount} lines)";
}
