using System.Text.RegularExpressions;

namespace Orchi.Api.Infrastructure.Agents.Plans;

public static partial class ReviewBriefBuilder
{
    public static string Build(
        string planId,
        string originalPlanMarkdown,
        Guid implementationChildChatId,
        Guid parentChatId)
    {
        return $"""
            # Review brief for plan {planId}

            ## Original implementation plan

            {originalPlanMarkdown.Trim()}

            ## Implementation chat

            Chat ID: `{implementationChildChatId}`

            ## Parent orchestration chat

            Chat ID: `{parentChatId}`

            ## Instructions

            Review the git diff against the original plan above.
            Focus on oversights, over-engineering, and missed patterns — not a restatement of the changes.
            Lead with a Review TLDR. Keep the review short and scannable.
            Produce one or more review plans using the exact format in your context section.
            """;
    }

    public static string BuildForBranchReview(
        string reviewId,
        string headBranch,
        string baseBranch)
    {
        return $"""
            # Review brief for branch `{headBranch}` vs `{baseBranch}`

            <!-- orchi-branch-review head: {headBranch} base: {baseBranch} -->

            ## Branch review

            - Head branch (changes to review): `{headBranch}`
            - Base branch (comparison target): `{baseBranch}`
            - Review id: `{reviewId}`

            ## Instructions

            This is a pull-request style review. There is no orchestration implementation plan.
            Review the changes introduced by the head branch relative to the base branch using the three-dot git diff injected into your prompt context.
            Focus on correctness, regressions, pattern fit, tests/validation, and risky edge cases.
            Produce one or more actionable review plans for the reviewer.
            """;
    }

    public static string ToBranchReviewId(string headBranch)
    {
        string slug = InvalidPlanIdChars()
            .Replace(headBranch.Trim().ToLowerInvariant(), "-")
            .Trim('-');

        while (slug.Contains("--", StringComparison.Ordinal))
        {
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            slug = Guid.NewGuid().ToString("N")[..8];
        }

        if (slug.Length > 40)
        {
            slug = slug[..40].TrimEnd('-');
        }

        return $"branch-{slug}";
    }

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex InvalidPlanIdChars();
}
