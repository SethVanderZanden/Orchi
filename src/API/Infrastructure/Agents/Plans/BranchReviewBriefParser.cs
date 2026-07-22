using System.Text.RegularExpressions;

namespace Orchi.Api.Infrastructure.Agents.Plans;

public static partial class BranchReviewBriefParser
{
    public sealed record BranchReviewRefs(string HeadBranch, string BaseBranch);

    public static BranchReviewRefs? TryParseFromFile(string workspacePath, string? relativeReviewPath)
    {
        if (string.IsNullOrWhiteSpace(relativeReviewPath))
        {
            return null;
        }

        string fullPath = Path.Combine(
            workspacePath,
            relativeReviewPath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
        {
            return null;
        }

        string content = File.ReadAllText(fullPath);
        return TryParse(content);
    }

    public static BranchReviewRefs? TryParse(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        Match match = BranchReviewMarkerPattern().Match(content);
        if (!match.Success)
        {
            return null;
        }

        string head = match.Groups["head"].Value.Trim();
        string baseBranch = match.Groups["base"].Value.Trim();
        if (string.IsNullOrWhiteSpace(head) || string.IsNullOrWhiteSpace(baseBranch))
        {
            return null;
        }

        return new BranchReviewRefs(head, baseBranch);
    }

    [GeneratedRegex(
        @"<!--\s*orchi-branch-review\s+head:\s*(?<head>[^\s]+)\s+base:\s*(?<base>[^\s]+)\s*-->",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BranchReviewMarkerPattern();
}
