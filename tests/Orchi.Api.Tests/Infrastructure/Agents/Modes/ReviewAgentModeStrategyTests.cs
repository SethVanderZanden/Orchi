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
        Assert.Contains("structured git-diff review", document.Identity);
        Assert.Contains("not a plan to review later", document.Identity);
        Assert.DoesNotContain("orchi-review-plan:id", document.Identity);
        Assert.Contains("Do not modify code unless the user explicitly asks", document.Rules);
        Assert.Contains("Complete the entire review in your first response", document.Rules);
        Assert.Contains("Do not stop after acknowledging", document.Rules);
        Assert.Contains("Walk through every changed file in the git diff", document.Rules);
        Assert.Contains("Required?", document.Rules);
        Assert.Contains("Over-engineered?", document.Rules);
        Assert.Contains("Review TLDR", document.Rules);
        Assert.Contains("very short summary of what was completed", document.Rules);
        Assert.Contains("primary goal or outcome", document.Rules);
        Assert.Contains("exactly what is missing", document.Rules);
        Assert.Contains("# Short title", document.Context);
        Assert.Contains("## Review TLDR", document.Context);
        Assert.Contains("**What was done:**", document.Context);
        Assert.Contains("## Changes", document.Context);
        Assert.Contains("**What changed:**", document.Context);
        Assert.Contains("## Cross-cutting findings", document.Context);
        Assert.Contains("### Oversights", document.Context);
        Assert.Contains("### Over-engineering", document.Context);
        Assert.Contains("### Missed patterns", document.Context);
        Assert.Contains("<orchi-open-editor>", document.Rules);
        Assert.Contains("<orchi-open-editor>", document.Context);
        Assert.Contains("code /workspace -g path/to/file:42", document.Context);
        Assert.DoesNotContain("{workspacePath}", document.Context);
        Assert.Null(document.Message);
    }

    [Fact]
    public void ModeId_IsReview()
    {
        Assert.Equal("review", _strategy.ModeId);
    }
}
