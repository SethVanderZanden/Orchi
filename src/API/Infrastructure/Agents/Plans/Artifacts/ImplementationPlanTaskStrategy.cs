using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

public sealed class ImplementationPlanTaskStrategy : IOrchiArtifactTaskStrategy
{
    public string Kind => OrchiArtifactKind.Plan;

    public string BuildTask(string relativePath) => PlanImplementationTask.Build(relativePath);
}
