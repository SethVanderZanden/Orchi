namespace Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

internal sealed class OrchiArtifactWriterFactory(IEnumerable<IOrchiArtifactWriterStrategy> strategies)
    : IOrchiArtifactWriterFactory
{
    private readonly Dictionary<string, IOrchiArtifactWriterStrategy> _strategies =
        strategies.ToDictionary(strategy => strategy.Kind, StringComparer.OrdinalIgnoreCase);

    public IOrchiArtifactWriterStrategy GetStrategy(string kind)
    {
        if (!_strategies.TryGetValue(kind, out IOrchiArtifactWriterStrategy? strategy))
        {
            throw new InvalidOperationException($"No artifact writer strategy registered for '{kind}'.");
        }

        return strategy;
    }

    public IOrchiArtifactWriterStrategy? TryGetStrategyFromPath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        string normalized = relativePath.Replace('\\', '/').ToLowerInvariant();

        if (normalized.Contains("/review-", StringComparison.Ordinal))
        {
            return GetStrategy(OrchiArtifactKind.Review);
        }

        if (normalized.Contains("/plan-", StringComparison.Ordinal))
        {
            return GetStrategy(OrchiArtifactKind.Plan);
        }

        return null;
    }
}
