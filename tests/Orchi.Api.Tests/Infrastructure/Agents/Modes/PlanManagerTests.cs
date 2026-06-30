using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

public class PlanManagerTests
{
    [Fact]
    public void ValidateAttachedPlan_FailsWhenMissing()
    {
        var manager = new PlanManager(new InMemoryPlanStore());

        Result result = manager.ValidateAttachedPlan(null);

        Assert.True(result.IsFailure);
        Assert.Equal("Plan.Required", result.Error.Code);
    }

    [Fact]
    public void CreatePlan_AndResolveContent_Succeeds()
    {
        var store = new InMemoryPlanStore();
        var manager = new PlanManager(store);
        Guid chatId = Guid.NewGuid();

        Result<PlanArtifact> created = manager.CreatePlan(chatId, "Feature", "Do the thing");
        Assert.True(created.IsSuccess);

        Result<string> content = manager.ResolvePlanContent(created.Value.Id);
        Assert.True(content.IsSuccess);
        Assert.Equal("Do the thing", content.Value);
    }
}
