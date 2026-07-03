using Orchi.Api.Infrastructure.Agents.Modes;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class OrchestrationAgentModeStrategyTests
{
    private readonly OrchestrationAgentModeStrategy _strategy = new();

    [Fact]
    public void BuildPrompt_ContainsOrchestrationGuidance()
    {
        string prompt = _strategy.BuildPrompt("Create an auth system");

        Assert.Contains("You are in Orchestration Mode.", prompt);
        Assert.Contains("enhanced plan mode", prompt);
        Assert.Contains("<!-- orchi-plan:kebab-case-id -->", prompt);
        Assert.Contains("Do not implement code yourself", prompt);
        Assert.Contains("User message:\nCreate an auth system", prompt);
    }

    [Fact]
    public void ModeId_IsOrchestration()
    {
        Assert.Equal("orchestration", _strategy.ModeId);
    }
}
