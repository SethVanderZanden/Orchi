using Orchi.Api.Infrastructure.Caching;

namespace Orchi.Api.Infrastructure.Agents.Workspace;

public sealed class CachingWorkspaceDiffProvider(
    IWorkspaceDiffProvider inner,
    OrchiHybridCacheService cache) : IWorkspaceDiffProvider
{
    public string GetDiff(string workspacePath)
    {
        string? headRevision = GitWorkspaceDiffProvider.TryGetHeadRevision(workspacePath);
        if (headRevision is null)
        {
            return inner.GetDiff(workspacePath);
        }

        string normalizedPath = Path.GetFullPath(workspacePath);
        string cacheKey = OrchiCacheKeys.WorkspaceDiff(normalizedPath, headRevision);

        // Sync bridge: prompt pipeline and IWorkspaceDiffProvider remain synchronous.
        // Acceptable for local HybridCache; Redis is not enabled yet.
        return cache.GetOrCreateAsync(
                cacheKey,
                _ => ValueTask.FromResult(inner.GetDiff(workspacePath)),
                cache.CreateWorkspaceDiffEntryOptions())
            .AsTask()
            .GetAwaiter()
            .GetResult();
    }

    public string GetBranchDiff(string workspacePath, string baseBranch, string headBranch)
    {
        string? headRevision = GitWorkspaceDiffProvider.TryGetHeadRevision(workspacePath);
        string normalizedPath = Path.GetFullPath(workspacePath);
        string cacheKey = OrchiCacheKeys.WorkspaceBranchDiff(
            normalizedPath,
            baseBranch.Trim(),
            headBranch.Trim(),
            headRevision ?? "unknown");

        return cache.GetOrCreateAsync(
                cacheKey,
                _ => ValueTask.FromResult(inner.GetBranchDiff(workspacePath, baseBranch, headBranch)),
                cache.CreateWorkspaceDiffEntryOptions())
            .AsTask()
            .GetAwaiter()
            .GetResult();
    }
}
