using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class ReviewAgentModeStrategyTests
{
    private readonly ReviewAgentModeStrategy _strategy = new();

    [Fact]
    public void ContributeSections_SetsWorkConductedIdentityAndSharedReviewPlumbing()
    {
        var document = new OrchiPromptDocument();
        var context = new PromptBuildContext
        {
            ModeId = ReviewAgentModeStrategy.Mode,
            UserContent = "Review the auth refactor implementation",
            WorkspacePath = "/workspace",
        };

        _strategy.ContributeSections(context, document);

        Assert.Contains("You are in Review Mode.", document.Identity);
        Assert.Contains("completed implementation work", document.Identity);
        Assert.Contains("original orchestration plan", document.Identity);
        Assert.Contains("not a plan to review later", document.Identity);
        Assert.DoesNotContain("orchi-review-plan:id", document.Identity);
        Assert.DoesNotContain("pull-request", document.Identity, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("original implementation plan", document.Rules);
        Assert.Contains("Do not modify code unless the user explicitly asks", document.Rules);
        Assert.Contains("Complete the entire review in your first response", document.Rules);
        Assert.Contains("Walk through every changed file in the git diff", document.Rules);
        Assert.Contains("Review TLDR", document.Rules);
        Assert.Contains("# Short title", document.Context);
        Assert.Contains("## Review TLDR", document.Context);
        Assert.Contains("## Changes", document.Context);
        Assert.Null(document.Message);
    }

    [Fact]
    public void ModeId_IsReview()
    {
        Assert.Equal("review", _strategy.ModeId);
        Assert.Contains("plan", _strategy.Description, StringComparison.OrdinalIgnoreCase);
    }
}
