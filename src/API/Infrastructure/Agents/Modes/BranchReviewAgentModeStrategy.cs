using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Modes;

/// <summary>
/// Branch / PR review: judge a head branch against a base branch.
/// Kickoff-only — same review pipeline as <see cref="ReviewAgentModeStrategy"/>; only identity/intent differ.
/// </summary>
public sealed class BranchReviewAgentModeStrategy : IAgentModeStrategy
{
    public const string Mode = AgentModeIds.BranchReview;

    internal const string Identity = """
        You are in Branch Review Mode.

        Produce a structured pull-request style review of the head branch against the base branch.
        There is no orchestration implementation plan — judge merge readiness, correctness, and design quality from the branch diff and brief.
        Walk through each changed file with a short explanation and judgment (required, clean, goal alignment, over-engineering).
        Output the review as markdown in your response — not a plan to review later.
        """;

    internal const string IntentRules = """
        Treat this like a PR review: what lands if head merges into base.
        Prefer the three-dot branch diff when present in context.
        """;

    public string ModeId => Mode;

    public string DisplayLabel => "Branch review";

    public string? Description => null;

    public IReadOnlyList<string> ExtraCliArgs => [];

    public void ContributeSections(PromptBuildContext context, OrchiPromptDocument document)
    {
        document.Identity = Identity;
        document.AppendRules(IntentRules);
        document.AppendRules(ReviewModeOutputFormat.CommonRules);
        document.AppendContext(ReviewModeOutputFormat.Context);
    }
}
