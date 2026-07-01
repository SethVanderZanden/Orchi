using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;
using Orchi.Api.Infrastructure.Agents.Modes.Strategies;
using Orchi.SharedContext.Modes;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class ImplementModeStrategyTests
{
    [Fact]
    public async Task PrepareTurnAsync_WithoutPlan_ReturnsFailure()
    {
        var composer = AgentPromptComposerTestFactory.Create(Directory.GetCurrentDirectory());
        var strategy = new ImplementModeStrategy(new PlanManager(new InMemoryPlanStore()), composer);
        var session = CreateSession(attachedPlanId: null);

        Result<AgentTurnRequest> result =
            await strategy.PrepareTurnAsync(session, "do work", new InMemoryPlanStore(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Plan.Required", result.Error.Code);
    }

    [Fact]
    public async Task PrepareTurnAsync_WithPlan_InjectsPlanContent()
    {
        var store = new InMemoryPlanStore();
        var planManager = new PlanManager(store);
        PlanArtifact plan = store.Create(Guid.NewGuid(), "Auth plan", "Step 1: add login");

        var strategy = new ImplementModeStrategy(
            planManager,
            AgentPromptComposerTestFactory.Create(Directory.GetCurrentDirectory()));
        var session = CreateSession(plan.Id);

        Result<AgentTurnRequest> result = await strategy.PrepareTurnAsync(session, "start", store, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("Step 1: add login", result.Value.PreparedPrompt);
        Assert.Empty(result.Value.ExtraCliArgs);
        Assert.Equal(CursorCliProfileKind.Agent, result.Value.CliProfileKind);
    }

    private static ChatSession CreateSession(Guid? attachedPlanId) =>
        new()
        {
            Id = Guid.NewGuid(),
            AgentId = "cursor",
            WorkspacePath = Directory.GetCurrentDirectory(),
            Mode = ChatMode.Implement,
            AttachedPlanId = attachedPlanId
        };
}
