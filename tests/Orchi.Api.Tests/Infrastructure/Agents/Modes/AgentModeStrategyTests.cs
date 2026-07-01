using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;
using Orchi.Api.Infrastructure.Agents.Modes.Strategies;
using Orchi.SharedContext.Modes;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class AgentModeStrategyTests
{
    [Fact]
    public async Task PrepareTurnAsync_UsesDefaultAgentWithoutModeFlag()
    {
        var strategy = new AgentModeStrategy(AgentPromptComposerTestFactory.Create(Directory.GetCurrentDirectory()));
        var session = CreateSession(ChatMode.Agent);
        var store = new InMemoryPlanStore();

        Result<AgentTurnRequest> result = await strategy.PrepareTurnAsync(session, "fix the bug", store, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.ExtraCliArgs);
        Assert.Equal(CursorCliProfileKind.Agent, result.Value.CliProfileKind);
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
