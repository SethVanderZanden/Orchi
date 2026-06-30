using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;
using Orchi.Api.Infrastructure.Agents.Modes.Strategies;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class PlanModeStrategyTests
{
    [Fact]
    public void PrepareTurn_AddsPlanModeFlag()
    {
        var strategy = new PlanModeStrategy();
        var session = CreateSession(ChatMode.Plan);
        var store = new InMemoryPlanStore();

        var result = strategy.PrepareTurn(session, "design auth", store);

        Assert.True(result.IsSuccess);
        Assert.Contains("--mode=plan", result.Value.ExtraCliArgs);
        Assert.Contains("design auth", result.Value.PreparedPrompt);
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
