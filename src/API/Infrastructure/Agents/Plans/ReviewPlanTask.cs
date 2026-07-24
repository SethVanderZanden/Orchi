namespace Orchi.Api.Infrastructure.Agents.Plans;

public static class ReviewPlanTask
{
    public static string Build(string reviewFilePath)
    {
        string path = reviewFilePath.Trim();
        return
            "Using the review brief and git diff in your context, produce the review now. " +
            "Walk through every changed file in the diff with explanation and judgment (required, clean, goal alignment, over-engineering). " +
            "Lead with a Review TLDR. Output one or more review plans using the exact format in your context section. " +
            "Do not modify code unless explicitly asked. " +
            $"After the review is complete, delete `{path}`. If blocked, keep the review brief file.";
    }
}
