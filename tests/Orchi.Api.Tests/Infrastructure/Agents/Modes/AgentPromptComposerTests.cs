using Orchi.Api.Infrastructure.Agents.Modes;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class AgentPromptComposerTests
{
    private readonly AgentPromptComposer _composer = new(new AgentModeStrategyFactory([
        new DefaultAgentModeStrategy(),
        new OrchestrationAgentModeStrategy()
    ]));

    [Fact]
    public void Compose_DefaultMode_ReturnsUserContentUnchanged()
    {
        const string userContent = "Build a login page";

        string prompt = _composer.Compose(DefaultAgentModeStrategy.Mode, userContent);

        Assert.Equal(userContent, prompt);
    }

    [Fact]
    public void Compose_OrchestrationMode_PrefixesStaticInstructions()
    {
        const string userContent = "Plan a refactor";

        string prompt = _composer.Compose(OrchestrationAgentModeStrategy.Mode, userContent);

        Assert.StartsWith(OrchestrationAgentModeStrategy.Instructions, prompt);
        Assert.EndsWith($"User message:\n{userContent}", prompt);
        Assert.Contains("---", prompt);
    }

    [Fact]
    public void GetExtraCliArgs_DefaultMode_ReturnsEmpty()
    {
        IReadOnlyList<string> args = _composer.GetExtraCliArgs(DefaultAgentModeStrategy.Mode);

        Assert.Empty(args);
    }
}
