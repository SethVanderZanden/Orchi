namespace Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

public interface IOrchiArtifactWriterFactory
{
    IOrchiArtifactWriterStrategy GetStrategy(string kind);

    IOrchiArtifactWriterStrategy? TryGetStrategyFromPath(string? relativePath);
}
