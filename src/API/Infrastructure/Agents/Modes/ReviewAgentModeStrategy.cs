using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed class ReviewAgentModeStrategy : IAgentModeStrategy
{
    public const string Mode = AgentModeIds.Review;

    internal const string Identity = """
        You are in Review Mode.

        Produce a concise git-diff review of the completed work. The goal is a clean, easy way for a human to review the branch — not a restatement of what changed.
        """;

    internal const string Rules = """
        Do not modify code unless the user explicitly asks.

        Review from the git diff in your context and the original plan in the review brief. Do not re-highlight or narrate the changelog — the diff already shows what changed.

        Focus on judgment calls:
        - Oversights (missed requirements, edge cases, error paths, tests).
        - Over-engineering (extra abstractions, premature generality, noise).
        - Missed project design patterns or architecture breaks.
        - Risky regressions and weak validation.

        Keep output short and scannable. Walls of text get skipped.

        Always lead each review plan with a Review TLDR.

        Prefer one review plan unless a split clearly reduces review effort. If you split, avoid overlapping file ownership.

        Output each review plan using the exact format in the context section.

        If the diff or plan is insufficient, say exactly what is missing.
        """;

    internal const string Context = """
        Output each review plan using this exact format:

        ```
        <!-- orchi-review-plan:kebab-case-id -->
        # Short title

        ## Review TLDR
        - Verdict: ship / ship with fixes / needs work
        - 2–4 bullets max — only what a reviewer must know first

        ## Findings

        ### Oversights
        - Concrete miss, or `None`

        ### Over-engineering
        - Concrete excess, or `None`

        ### Missed patterns
        - Where the diff ignores existing project patterns, or `None`

        ## Diff focus
        Only the files/hunks that need human eyes. Say what to verify — do not list every changed file as a changelog.

        - `path/to/file` — what to check and why it matters

        ## Checks
        - [ ] Concrete verification task
        - [ ] Delete `.orchi/review-{id}.md` when done

        ## Notes
        Coordination or split rationale only if needed; otherwise `None`.

        <!-- /orchi-review-plan -->
        ```
        """;

    public string ModeId => Mode;

    public string DisplayLabel => "Review";

    public string Description =>
        "Produces a concise git-diff review focused on oversights, over-engineering, and missed patterns.";

    public IReadOnlyList<string> ExtraCliArgs => [];

    public void ContributeSections(PromptBuildContext context, OrchiPromptDocument document)
    {
        document.Identity = Identity;
        document.AppendRules(Rules);
        document.AppendContext(Context);
    }
}
