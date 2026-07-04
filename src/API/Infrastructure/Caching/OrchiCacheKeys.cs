namespace Orchi.Api.Infrastructure.Caching;

public static class OrchiCacheKeys
{
    public static string WorkspaceDiff(string normalizedWorkspacePath, string gitHeadRevision) =>
        $"workspace-diff:{NormalizePath(normalizedWorkspacePath)}:{gitHeadRevision}";

    public static string CursorExecutable(string configFingerprint) =>
        $"cursor-exec:{configFingerprint}";

    public static string Plan(Guid sourceChatId, string planId) =>
        $"plan:{sourceChatId:N}:{planId}";

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
