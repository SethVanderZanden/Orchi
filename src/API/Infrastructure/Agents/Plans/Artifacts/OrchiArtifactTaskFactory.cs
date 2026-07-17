namespace Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

internal sealed class OrchiArtifactTaskFactory(
    IEnumerable<IOrchiArtifactTaskStrategy> strategies,
    IOrchiArtifactWriterFactory writerFactory) : IOrchiArtifactTaskFactory
{
    private readonly Dictionary<string, IOrchiArtifactTaskStrategy> _strategies =
        strategies.ToDictionary(strategy => strategy.Kind, StringComparer.OrdinalIgnoreCase);

    public IOrchiArtifactTaskStrategy GetStrategy(string kind)
    {
        if (!_strategies.TryGetValue(kind, out IOrchiArtifactTaskStrategy? strategy))
        {
            throw new InvalidOperationException($"No artifact task strategy registered for '{kind}'.");
        }

        return strategy;
    }

    public string? ResolveTaskFromPath(string? relativePath)
    {
        IOrchiArtifactWriterStrategy? writerStrategy = writerFactory.TryGetStrategyFromPath(relativePath);
        if (writerStrategy is null || string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        return GetStrategy(writerStrategy.Kind).BuildTask(relativePath);
    }
}
