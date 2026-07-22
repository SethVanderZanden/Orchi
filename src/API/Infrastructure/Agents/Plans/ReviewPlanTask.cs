namespace Orchi.Api.Infrastructure.Agents.Plans;

public static class ReviewPlanTask
{
    public static string Build(string reviewFilePath)
    {
        string path = reviewFilePath.Trim();
        return
            $"Review the implementation described in `{path}` using the git diff in your context. " +
            "Compare against the original plan in the review brief. " +
            "Focus on oversights, over-engineering, and missed patterns — do not restate the changelog. " +
            "Lead with a Review TLDR. Output one or more review plans using the exact format in your context section. " +
            "Do not modify code unless explicitly asked. " +
            $"After the review is complete, delete `{path}`. If blocked, keep the review brief file.";
    }
}
