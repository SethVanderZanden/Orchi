namespace Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

public interface IOrchiArtifactTaskFactory
{
    IOrchiArtifactTaskStrategy GetStrategy(string kind);

    string? ResolveTaskFromPath(string? relativePath);
}
