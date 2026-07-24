using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Orchi.Api.Infrastructure.Git.Workspace;

/// <summary>
/// Resolves git worktree checkout paths outside deep repository trees so agent CLIs
/// stay under Windows MAX_PATH limits while keeping reviews isolated from the primary workspace.
/// </summary>
public static partial class GitWorktreePathResolver
{
    public const int MaxSegmentLength = 24;

    public static string ResolveWorktreePath(string repoRoot, string worktreeSegmentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(worktreeSegmentId);

        string safeId = SanitizeWorktreeSegment(worktreeSegmentId);
        string repoKey = ComputeRepoKey(repoRoot);
        string worktreesRoot = Path.Combine(GetOrchiWorktreesRoot(), repoKey);
        Directory.CreateDirectory(worktreesRoot);
        return Path.Combine(worktreesRoot, safeId);
    }

    public static string NewOpaqueWorktreeSegmentId() => Guid.NewGuid().ToString("N")[..12];

    private static string GetOrchiWorktreesRoot()
    {
        string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(baseDir))
        {
            baseDir = Path.GetTempPath();
        }

        return Path.Combine(baseDir, "Orchi", "worktrees");
    }

    private static string ComputeRepoKey(string repoRoot)
    {
        string normalized = Path.GetFullPath(repoRoot)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }

    private static string SanitizeWorktreeSegment(string value)
    {
        string trimmed = value.Trim();
        if (trimmed.Length > MaxSegmentLength)
        {
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(trimmed));
            return Convert.ToHexString(hash)[..12].ToLowerInvariant();
        }

        string sanitized = InvalidPathChars().Replace(trimmed, "-");
        return string.IsNullOrWhiteSpace(sanitized)
            ? NewOpaqueWorktreeSegmentId()
            : sanitized;
    }

    [GeneratedRegex(@"[^a-zA-Z0-9._-]+")]
    private static partial Regex InvalidPathChars();
}
