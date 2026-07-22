namespace Orchi.Api.Infrastructure.Agents.Plans;

public static class ReviewBriefBuilder
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
}
