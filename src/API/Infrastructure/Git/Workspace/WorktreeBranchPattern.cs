using System.Globalization;
using System.Text.RegularExpressions;

namespace Orchi.Api.Infrastructure.Git.Workspace;

/// <summary>
/// Resolves worktree branch names from a project pattern.
/// Tokens: <c>{date}</c>, <c>{time}</c>, <c>{shortId}</c>, <c>{chatId}</c>, <c>{mode}</c>.
/// </summary>
public static partial class WorktreeBranchPattern
{
    public const string Default = "orchi/{date}-{shortId}";

    public static string Resolve(string? pattern, Guid chatId, string? mode)
    {
        string template = string.IsNullOrWhiteSpace(pattern) ? Default : pattern.Trim();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string shortId = Guid.NewGuid().ToString("N")[..8];
        string chatShort = chatId.ToString("N")[..8];
        string modeToken = string.IsNullOrWhiteSpace(mode) ? "default" : mode.Trim().ToLowerInvariant();

        string resolved = template
            .Replace("{date}", now.ToString("yyyyMMdd", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{time}", now.ToString("HHmmss", CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase)
            .Replace("{shortId}", shortId, StringComparison.OrdinalIgnoreCase)
            .Replace("{chatId}", chatShort, StringComparison.OrdinalIgnoreCase)
            .Replace("{mode}", modeToken, StringComparison.OrdinalIgnoreCase);

        return SanitizeBranch(resolved);
    }

    private static string SanitizeBranch(string value)
    {
        string trimmed = value.Trim().Trim('/');
        string sanitized = InvalidBranchChars().Replace(trimmed, "-");
        sanitized = CollapseDashes().Replace(sanitized, "-");
        return string.IsNullOrWhiteSpace(sanitized) ? $"orchi/{Guid.NewGuid():N}"[..16] : sanitized;
    }

    [GeneratedRegex(@"[^a-zA-Z0-9._/-]+")]
    private static partial Regex InvalidBranchChars();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex CollapseDashes();
}
