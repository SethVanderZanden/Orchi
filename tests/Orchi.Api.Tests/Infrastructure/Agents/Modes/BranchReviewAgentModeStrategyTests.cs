using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class BranchReviewAgentModeStrategyTests
{
    private readonly BranchReviewAgentModeStrategy _strategy = new();

    [Fact]
    public void ContributeSections_SetsPrFocusedIdentityRulesAndSharedContext()
    {
        var document = new OrchiPromptDocument();
        var context = new PromptBuildContext
        {
            ModeId = BranchReviewAgentModeStrategy.Mode,
            UserContent = "Begin review.",
            WorkspacePath = "/workspace",
        };

        _strategy.ContributeSections(context, document);

        Assert.Contains("You are in Branch Review Mode.", document.Identity);
        Assert.Contains("pull-request style review", document.Identity);
        Assert.Contains("no orchestration implementation plan", document.Identity);
        Assert.Contains("Do not modify code unless the user explicitly asks", document.Rules);
        Assert.Contains("Treat this like a PR review", document.Rules);
        Assert.Contains("Merge readiness", document.Rules);
        Assert.Contains("Review TLDR", document.Rules);
        Assert.Contains("# Short title", document.Context);
        Assert.Contains("## Review TLDR", document.Context);
        Assert.Contains("## Changes", document.Context);
        Assert.Null(document.Message);
    }

    [Fact]
    public void ModeId_IsBranchReview()
    {
        Assert.Equal("branch-review", _strategy.ModeId);
        Assert.Equal("Branch review", _strategy.DisplayLabel);
        Assert.Null(_strategy.Description);
    }
}
