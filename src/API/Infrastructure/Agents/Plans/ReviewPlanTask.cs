namespace Orchi.Api.Infrastructure.Agents.Plans;

public static class ReviewPlanTask
{
    public static string Build(string reviewFilePath)
    {
        string path = reviewFilePath.Trim();
        return
            "Using the review brief and git diff in your context, walk through every changed file and produce the structured review now. " +
            "Lead with a Review TLDR. Write the full review in your response — do not draft a separate plan to review later. " +
            "Do not modify code unless explicitly asked. " +
            $"After the review is complete, delete `{path}`. If blocked, keep the review brief file.";
    }
}
