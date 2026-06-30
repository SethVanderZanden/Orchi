using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;
using Orchi.Api.Infrastructure.Agents.Modes.Strategies;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class ImplementModeStrategyTests
{
    [Fact]
    public void PrepareTurn_WithoutPlan_ReturnsFailure()
    {
        var strategy = new ImplementModeStrategy(new PlanManager(new InMemoryPlanStore()));
        var session = CreateSession(attachedPlanId: null);

        var result = strategy.PrepareTurn(session, "do work", new InMemoryPlanStore());

        Assert.True(result.IsFailure);
        Assert.Equal("Plan.Required", result.Error.Code);
    }

    [Fact]
    public void PrepareTurn_WithPlan_InjectsPlanContent()
    {
        var store = new InMemoryPlanStore();
        var planManager = new PlanManager(store);
        PlanArtifact plan = store.Create(Guid.NewGuid(), "Auth plan", "Step 1: add login");

        var strategy = new ImplementModeStrategy(planManager);
        var session = CreateSession(plan.Id);

        var result = strategy.PrepareTurn(session, "start", store);

        Assert.True(result.IsSuccess);
        Assert.Contains("Step 1: add login", result.Value.PreparedPrompt);
        Assert.Empty(result.Value.ExtraCliArgs);
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
