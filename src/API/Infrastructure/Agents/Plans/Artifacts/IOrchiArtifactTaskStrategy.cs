namespace Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

public interface IOrchiArtifactTaskStrategy
{
    string Kind { get; }

    string BuildTask(string relativePath);
}
