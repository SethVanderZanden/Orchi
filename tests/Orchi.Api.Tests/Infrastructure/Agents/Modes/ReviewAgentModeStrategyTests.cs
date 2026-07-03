using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class ReviewAgentModeStrategyTests
{
    private readonly ReviewAgentModeStrategy _strategy = new();

    [Fact]
    public void ContributeSections_SetsIdentityRulesAndContext()
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
        Assert.Contains("post-implementation code review planning mode", document.Identity);
        Assert.Contains("Do not modify code unless the user explicitly asks", document.Rules);
        Assert.Contains("missing information needed", document.Rules);
        Assert.Contains("<!-- orchi-review-plan:kebab-case-id -->", document.Context);
        Assert.Contains("Plan comparison and drift checks", document.Context);
        Assert.Null(document.Message);
    }

    [Fact]
    public void ModeId_IsReview()
    {
        Assert.Equal("review", _strategy.ModeId);
    }
}
