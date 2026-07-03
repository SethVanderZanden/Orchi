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

            Review the implementation against the original plan above using the git diff injected into your prompt context.
            Produce one or more actionable review plans for the reviewer.
            """;
    }
}
