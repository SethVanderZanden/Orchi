namespace Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

public sealed class ImplementationPlanWriterStrategy(OrchiArtifactFileStore fileStore) : IOrchiArtifactWriterStrategy
{
    public string Kind => OrchiArtifactKind.Plan;

    public string BuildRelativePath(string planId)
    {
        string sanitizedPlanId = OrchiArtifactFileStore.SanitizePlanId(planId);
        return $".orchi/plan-{sanitizedPlanId}.md";
    }

    public Task<string> WriteAsync(
        string workspacePath,
        string planId,
        string contentMarkdown,
        CancellationToken cancellationToken = default) =>
        fileStore.WriteAsync(workspacePath, BuildRelativePath(planId), contentMarkdown, cancellationToken);
}
