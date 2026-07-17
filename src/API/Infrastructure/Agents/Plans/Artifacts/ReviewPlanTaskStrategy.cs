using Orchi.Api.Infrastructure.Agents.Plans;

namespace Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

public sealed class ReviewPlanTaskStrategy : IOrchiArtifactTaskStrategy
{
    public string Kind => OrchiArtifactKind.Review;

    public string BuildTask(string relativePath) => ReviewPlanTask.Build(relativePath);
}
