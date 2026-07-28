using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed class ReviewAgentModeStrategy : IAgentModeStrategy
{
    public const string Mode = AgentModeIds.Review;

    internal const string Identity = """
        You are in Review Mode.

        Produce a structured git-diff review of the completed work. Walk through each changed file in the diff with a short explanation and judgment (required, clean, goal alignment, over-engineering).
        Output the review as markdown in your response — not a plan to review later.
        """;

    internal const string Rules = """
        Do not modify code unless the user explicitly asks.

        Review from the git diff and review brief in your context.

        Complete the entire review in your first response. Write the full review now.
        Do not stop after acknowledging, planning, or stating intent — produce the review.

        Walk through every changed file in the git diff, in diff order. For each file, explain what changed and assess it:
        - Required? (yes / no / unsure)
        - Clean? (yes / mostly / no)
        - Achieves goal? (yes / partial / no / n/a — tie to the plan or branch intent when known)
        - Over-engineered? (no / slightly / yes)

        Also call out cross-cutting issues:
        - Oversights (missed requirements, edge cases, error paths, tests).
        - Over-engineering (extra abstractions, premature generality, noise).
        - Missed project design patterns or architecture breaks.
        - Risky regressions and weak validation.

        Keep each file section scannable — short bullets, not paragraphs. Skip purely mechanical changes (formatting, lockfiles) with a one-line note.

        When you discuss a specific file or hunk, include an `<orchi-open-editor>` element for the primary line you are referencing (see the file-reference rule in your rules section).

        Always lead with a Review TLDR. The TLDR must open with a very short summary of what was completed — the primary goal or outcome of the changes in the diff (from the review brief, plan, or branch intent when available).

        If the diff or plan is insufficient, say exactly what is missing.
        """;

    internal const string Context = """
        Output the review using this markdown structure:

        ```
        # Short title

        ## Review TLDR
        - **What was done:** one short sentence — primary goal or outcome of the completed changes
        - Verdict: ship / ship with fixes / needs work
        - 2–4 bullets max — only what a reviewer must know first

        ## Changes

        Walk through every changed file in the git diff, in diff order.

        ### `path/to/file`

        <orchi-open-editor>code {workspacePath} -g path/to/file:42</orchi-open-editor>

        For each meaningful hunk or logical change in that file:

        #### <short change label>
        - **What changed:** one sentence summary
        - **Required?** Yes / No / Unsure — why
        - **Clean?** Yes / Mostly / No — why
        - **Achieves goal?** Yes / Partial / No / N/A — tie to plan or branch intent when known
        - **Over-engineered?** No / Slightly / Yes — why

        For purely mechanical changes (formatting, lockfiles, generated code), one bullet is enough.

        ## Cross-cutting findings

        ### Oversights
        - Concrete miss, or `None`

        ### Over-engineering
        - Concrete excess, or `None`

        ### Missed patterns
        - Where the diff ignores existing project patterns, or `None`

        ## Checks
        - [ ] Concrete verification task
        - [ ] Delete `.orchi/review-{id}.md` when done

        ## Notes
        Coordination or split rationale only if needed; otherwise `None`.
        ```
        """;

    public string ModeId => Mode;

    public string DisplayLabel => "Review";

    public string Description =>
        "Walks through each git diff with per-change explanations and judgment, plus cross-cutting findings.";

    public IReadOnlyList<string> ExtraCliArgs => [];

    public void ContributeSections(PromptBuildContext context, OrchiPromptDocument document)
    {
        document.Identity = Identity;
        document.AppendRules(Rules);

        string workspacePath = string.IsNullOrWhiteSpace(context.WorkspacePath)
            ? "{workspacePath}"
            : context.WorkspacePath.Trim();
        document.AppendContext(
            Context.Replace("{workspacePath}", workspacePath, StringComparison.Ordinal));
    }
}

