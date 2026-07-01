using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;
using Orchi.Api.Infrastructure.Agents.Modes.Strategies;
using Orchi.SharedContext.Modes;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class ParticipantModeStrategyTests
{
    [Fact]
    public async Task PrepareTurnAsync_UsesAskModeAndIncludesUserContent()
    {
        var strategy = new ParticipantModeStrategy(AgentPromptComposerTestFactory.Create(Directory.GetCurrentDirectory()));
        var session = CreateSession(ChatMode.Participant);
        var store = new InMemoryPlanStore();

        Result<AgentTurnRequest> result =
            await strategy.PrepareTurnAsync(session, "feature X works like this", store, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(["--mode=ask"], result.Value.ExtraCliArgs);
        Assert.Equal(CursorCliProfileKind.Ask, result.Value.CliProfileKind);
        Assert.Contains("feature X works like this", result.Value.PreparedPrompt);
        Assert.Contains("participant in this chat", result.Value.PreparedPrompt, StringComparison.OrdinalIgnoreCase);
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
