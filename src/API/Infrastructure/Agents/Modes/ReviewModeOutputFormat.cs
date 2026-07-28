namespace Orchi.Api.Infrastructure.Agents.Modes;

/// <summary>
/// Shared review prompt pieces. Mode strategies only swap identity / intent;
/// contributors, adapters, and output shape stay the same for both review modes.
/// </summary>
internal static class ReviewModeOutputFormat
{
    internal const string CommonRules = """
        Do not modify code unless the user explicitly asks.

        Review from the git diff and review brief in your context.

        Complete the entire review in your first response. Write the full review now.
        Do not stop after acknowledging, planning, or stating intent — produce the review.

        Walk through every changed file in the git diff, in diff order. For each file, explain what changed and assess it:
        - Required? (yes / no / unsure)
        - Clean? (yes / mostly / no)
        - Achieves goal? (yes / partial / no / n/a — tie to the review brief when known)
        - Over-engineered? (no / slightly / yes)

        Also call out cross-cutting issues:
        - Oversights (missed requirements, edge cases, error paths, tests).
        - Over-engineering (extra abstractions, premature generality, noise).
        - Missed project design patterns or architecture breaks.
        - Risky regressions and weak validation.

        Keep each file section scannable — short bullets, not paragraphs. Skip purely mechanical changes (formatting, lockfiles) with a one-line note.

        Always lead with a Review TLDR.

        If the diff or brief is insufficient, say exactly what is missing.
        """;

    internal const string Context = """
        Output the review using this markdown structure:

        ```
        # Short title

        ## Review TLDR
        - Verdict: ship / ship with fixes / needs work
        - 2–4 bullets max — only what a reviewer must know first

        ## Changes

        Walk through every changed file in the git diff, in diff order.

        ### `path/to/file`

        For each meaningful hunk or logical change in that file:

        #### <short change label>
        - **What changed:** one sentence summary
        - **Required?** Yes / No / Unsure — why
        - **Clean?** Yes / Mostly / No — why
        - **Achieves goal?** Yes / Partial / No / N/A — tie to the review brief when known
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
}
