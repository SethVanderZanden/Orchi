namespace Orchi.Api.Infrastructure.Agents.Plans;

public static class ReviewPlanTask
{
    public static string Build(string reviewFilePath)
    {
        string path = reviewFilePath.Trim();
        return
            $"Review the implementation described in `{path}`. The original plan is in the review brief file. " +
            "Implementation changes are provided in your context section (git diff). Compare the diff against the plan " +
            "and expected behavior. Output one or more review plans using the exact format described in your context section. " +
            "Do not modify code unless explicitly asked. " +
            $"After the review is complete, delete `{path}`. If blocked, keep the review brief file.";
    }
}
