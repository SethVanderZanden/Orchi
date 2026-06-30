using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;
using Orchi.Api.Infrastructure.Agents.Modes.Strategies;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class AgentModeStrategyTests
{
    [Fact]
    public void PrepareTurn_UsesDefaultAgentWithoutModeFlag()
    {
        var strategy = new AgentModeStrategy();
        var session = CreateSession(ChatMode.Agent);
        var store = new InMemoryPlanStore();

        var result = strategy.PrepareTurn(session, "fix the bug", store);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.ExtraCliArgs);
        Assert.Contains("fix the bug", result.Value.PreparedPrompt);
    }

    private static ChatSession CreateSession(ChatMode mode) =>
        new()
        {
            Id = Guid.NewGuid(),
            AgentId = "cursor",
            WorkspacePath = Directory.GetCurrentDirectory(),
            Mode = mode
        };
}
