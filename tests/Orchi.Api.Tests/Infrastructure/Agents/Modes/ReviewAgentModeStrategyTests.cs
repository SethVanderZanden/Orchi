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
        Assert.Contains("concise git-diff review", document.Identity);
        Assert.Contains("Do not modify code unless the user explicitly asks", document.Rules);
        Assert.Contains("Review TLDR", document.Rules);
        Assert.Contains("exactly what is missing", document.Rules);
        Assert.Contains("<!-- orchi-review-plan:kebab-case-id -->", document.Context);
        Assert.Contains("## Review TLDR", document.Context);
        Assert.Contains("### Oversights", document.Context);
        Assert.Contains("### Over-engineering", document.Context);
        Assert.Contains("### Missed patterns", document.Context);
        Assert.Null(document.Message);
    }

    [Fact]
    public void ModeId_IsReview()
    {
        Assert.Equal("review", _strategy.ModeId);
    }
}
