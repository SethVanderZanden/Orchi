namespace Orchi.Api.Infrastructure.Caching;

public static class OrchiCacheKeys
{
    public static string WorkspaceDiff(string normalizedWorkspacePath, string gitHeadRevision) =>
        $"workspace-diff:{NormalizePath(normalizedWorkspacePath)}:{gitHeadRevision}";

    public static string WorkspaceBranchDiff(
        string normalizedWorkspacePath,
        string baseBranch,
        string headBranch,
        string gitHeadRevision) =>
        $"workspace-branch-diff:{NormalizePath(normalizedWorkspacePath)}:{baseBranch}:{headBranch}:{gitHeadRevision}";

    public static string CursorExecutable(string configFingerprint) =>
        $"cursor-exec:{configFingerprint}";

    public static string Plan(Guid sourceChatId, string planId) =>
        $"plan:{sourceChatId:N}:{planId}";

    public static string AgentModels(string agentId, bool includeDisabled) =>
        $"agent-models:{agentId}:{includeDisabled}";

    public static string AgentContextSizes(string agentId, bool includeDisabled) =>
        $"agent-context-sizes:{agentId}:{includeDisabled}";

    public static string AgentCliOptions(string agentId, string kind, bool includeDisabled) =>
        $"agent-cli-options:{agentId}:{kind}:{includeDisabled}";

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
