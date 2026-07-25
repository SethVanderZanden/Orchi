using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Modes;

/// <summary>
/// Branch / PR review: judge a head branch against a base branch (pull-request style).
/// Kickoff-only — not shown in the user mode cycle.
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

    internal const string Rules = """
        Do not modify code unless the user explicitly asks.

        Review from the three-dot git diff and branch review brief in your context.
        Treat this like a PR review: what lands if head merges into base.

        Complete the entire review in your first response. Write the full review now.
        Do not stop after acknowledging, planning, or stating intent — produce the review.

        Walk through every changed file in the git diff, in diff order. For each file, explain what changed and assess it:
        - Required? (yes / no / unsure) — relative to the branch / PR intent when known
        - Clean? (yes / mostly / no)
        - Achieves goal? (yes / partial / no / n/a — tie to branch intent when known)
        - Over-engineered? (no / slightly / yes)

        Also call out cross-cutting issues:
        - Oversights (missing tests, API/contract breaks, edge cases, error paths).
        - Over-engineering (extra abstractions, premature generality, noise).
        - Missed project design patterns or architecture breaks.
        - Risky regressions, migration hazards, and weak validation.
        - Merge readiness (conflicts with base intent, incomplete feature surface, docs/changelog gaps when relevant).

        Keep each file section scannable — short bullets, not paragraphs. Skip purely mechanical changes (formatting, lockfiles) with a one-line note.

        Always lead with a Review TLDR that includes a ship / ship with fixes / needs work verdict.

        If the diff or branch brief is insufficient, say exactly what is missing.
        """;

    public string ModeId => Mode;

    public string DisplayLabel => "Branch review";

    public string? Description => null;

    public IReadOnlyList<string> ExtraCliArgs => [];

    public void ContributeSections(PromptBuildContext context, OrchiPromptDocument document)
    {
        document.Identity = Identity;
        document.AppendRules(Rules);
        document.AppendContext(ReviewModeOutputFormat.Context);
    }
}
