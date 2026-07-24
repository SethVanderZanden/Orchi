using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Tests.Infrastructure.Agents.Plans;

public class ReviewPlanTaskTests
{
    [Fact]
    public void Build_IncludesReviewAndDeleteInstructions()
    {
        const string reviewPath = ".orchi/review-auth.md";

        string task = ReviewPlanTask.Build(reviewPath);

        Assert.Contains("review brief and git diff in your context", task);
        Assert.Contains("produce the structured review now", task);
        Assert.Contains("do not draft a separate plan to review later", task, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("review plans", task, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("git diff", task, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Review TLDR", task);
        Assert.Contains($"delete `{reviewPath}`", task);
        Assert.Contains("If blocked, keep the review brief file", task);
    }
}
