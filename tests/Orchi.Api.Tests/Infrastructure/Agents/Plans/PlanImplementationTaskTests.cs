using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Tests.Infrastructure.Agents.Plans;

public class PlanImplementationTaskTests
{
    [Fact]
    public void Build_IncludesImplementAndDeleteInstructions()
    {
        const string planPath = ".orchi/plan-auth.md";

        string task = PlanImplementationTask.Build(planPath);

        Assert.Contains($"Implement the plan at `{planPath}`", task);
        Assert.Contains("Follow the plan precisely", task);
        Assert.Contains($"delete `{planPath}`", task);
        Assert.Contains("If blocked, keep the plan file", task);
    }

    [Fact]
    public void Build_TrimsPlanFilePath()
    {
        string task = PlanImplementationTask.Build("  .orchi/plan-auth.md  ");

        Assert.Contains("`.orchi/plan-auth.md`", task);
        Assert.DoesNotContain("  .orchi/plan-auth.md  ", task);
    }
}
