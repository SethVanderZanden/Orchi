using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;
using Orchi.Api.Infrastructure.Agents.Modes.Strategies;
using Orchi.SharedContext.Modes;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class PlanModeStrategyTests
{
    [Fact]
    public async Task PrepareTurnAsync_AddsPlanModeFlag()
    {
        var strategy = new PlanModeStrategy(AgentPromptComposerTestFactory.Create(Directory.GetCurrentDirectory()));
        var session = CreateSession(ChatMode.Plan);
        var store = new InMemoryPlanStore();

        Result<AgentTurnRequest> result = await strategy.PrepareTurnAsync(session, "design auth", store, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("--mode=plan", result.Value.ExtraCliArgs);
        Assert.Equal(CursorCliProfileKind.Plan, result.Value.CliProfileKind);
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
