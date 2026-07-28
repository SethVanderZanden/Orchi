namespace Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

public sealed class ReviewBriefWriterStrategy(OrchiArtifactFileStore fileStore) : IOrchiArtifactWriterStrategy
{
    public string Kind => OrchiArtifactKind.Review;

    public string BuildRelativePath(string planId)
    {
        string sanitizedPlanId = OrchiArtifactFileStore.SanitizePlanId(planId);
        return $".orchi/review-{sanitizedPlanId}.md";
    }

    public Task<string> WriteAsync(
        string workspacePath,
        string planId,
        string contentMarkdown,
        CancellationToken cancellationToken = default) =>
        fileStore.WriteAsync(workspacePath, BuildRelativePath(planId), contentMarkdown, cancellationToken);

    public Task<bool> TryDeleteAsync(
        string workspacePath,
        string planId,
        CancellationToken cancellationToken = default) =>
        fileStore.TryDeleteAsync(workspacePath, BuildRelativePath(planId), cancellationToken);
}
