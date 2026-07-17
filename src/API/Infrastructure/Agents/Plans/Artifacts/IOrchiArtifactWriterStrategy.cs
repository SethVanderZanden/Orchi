namespace Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

public interface IOrchiArtifactWriterStrategy
{
    string Kind { get; }

    string BuildRelativePath(string planId);

    Task<string> WriteAsync(
        string workspacePath,
        string planId,
        string contentMarkdown,
        CancellationToken cancellationToken = default);
}
