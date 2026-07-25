using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Modes;

/// <summary>
/// Work-conducted review: judge completed implementation against its orchestration plan.
/// Shares review pipeline plumbing with <see cref="BranchReviewAgentModeStrategy"/>.
/// </summary>
public sealed class ReviewAgentModeStrategy : IAgentModeStrategy
{
    public const string Mode = AgentModeIds.Review;

    internal const string Identity = """
        You are in Review Mode.

        Produce a structured git-diff review of the completed implementation work.
        Judge the diff against the original orchestration plan in the review brief.
        Walk through each changed file with a short explanation and judgment (required, clean, goal alignment, over-engineering).
        Output the review as markdown in your response — not a plan to review later.
        """;

    internal const string IntentRules = """
        The brief includes the original implementation plan — treat that plan as the source of truth for intent and scope.
        """;

    public string ModeId => Mode;

    public string DisplayLabel => "Review";

    public string Description =>
        "Reviews completed implementation work against its plan, with per-change judgment and cross-cutting findings.";

    public IReadOnlyList<string> ExtraCliArgs => [];

    public void ContributeSections(PromptBuildContext context, OrchiPromptDocument document)
    {
        document.Identity = Identity;
        document.AppendRules(IntentRules);
        document.AppendRules(ReviewModeOutputFormat.CommonRules);
        document.AppendContext(ReviewModeOutputFormat.Context);
    }
}
