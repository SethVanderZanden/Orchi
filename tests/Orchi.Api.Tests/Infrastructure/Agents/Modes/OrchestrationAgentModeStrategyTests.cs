using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class OrchestrationAgentModeStrategyTests
{
    private readonly OrchestrationAgentModeStrategy _strategy = new();

    [Fact]
    public void ContributeSections_SetsIdentityRulesAndContext()
    {
        var document = new OrchiPromptDocument();
        var context = new PromptBuildContext
        {
            ModeId = OrchestrationAgentModeStrategy.Mode,
            UserContent = "Create an auth system",
            WorkspacePath = "/workspace",
        };

        _strategy.ContributeSections(context, document);

        Assert.Contains("You are in Orchestration Mode.", document.Identity);
        Assert.Contains("enhanced planning mode", document.Identity);
        Assert.Contains("Do not implement code yourself", document.Rules);
        Assert.Contains("no plan can be formed", document.Rules);
        Assert.Contains("<!-- orchi-plan:kebab-case-id -->", document.Context);
        Assert.Contains("<!-- orchi-plan-sequence -->", document.Context);
        Assert.Contains("orchi-plan-sequence", document.Rules);
        Assert.Null(document.Message);
    }

    [Fact]
    public void ModeId_IsOrchestration()
    {
        Assert.Equal("orchestration", _strategy.ModeId);
    }
}
