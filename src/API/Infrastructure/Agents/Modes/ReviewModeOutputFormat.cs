namespace Orchi.Api.Infrastructure.Agents.Modes;

/// <summary>
/// Shared markdown shape for work-conducted review and branch/PR review so desktop parsers stay aligned.
/// </summary>
internal static class ReviewModeOutputFormat
{
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
