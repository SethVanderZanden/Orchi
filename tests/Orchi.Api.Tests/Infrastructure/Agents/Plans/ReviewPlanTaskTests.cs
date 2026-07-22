using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Tests.Infrastructure.Agents.Plans;

public class ReviewPlanTaskTests
{
    [Fact]
    public void Build_IncludesReviewAndDeleteInstructions()
    {
        const string reviewPath = ".orchi/review-auth.md";

        string task = ReviewPlanTask.Build(reviewPath);

        Assert.Contains($"Review `{reviewPath}`", task);
        Assert.Contains("Output one or more review plans", task);
        Assert.Contains("git diff", task, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Review TLDR", task);
        Assert.Contains("do not restate the changelog", task);
        Assert.Contains($"delete `{reviewPath}`", task);
        Assert.Contains("If blocked, keep the review brief file", task);
    }
}
