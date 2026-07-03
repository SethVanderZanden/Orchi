namespace Orchi.Api.Infrastructure.Agents.Modes;

internal sealed class AgentModeStrategyFactory(IEnumerable<IAgentModeStrategy> strategies) : IAgentModeStrategyFactory
{
    private readonly Dictionary<string, IAgentModeStrategy> _strategies =
        strategies.ToDictionary(strategy => strategy.ModeId, StringComparer.OrdinalIgnoreCase);

    public IAgentModeStrategy GetStrategy(string modeId)
    {
        string resolvedMode = string.IsNullOrWhiteSpace(modeId) ? DefaultAgentModeStrategy.Mode : modeId;

        if (!_strategies.TryGetValue(resolvedMode, out IAgentModeStrategy? strategy))
        {
            throw new InvalidOperationException($"No agent mode strategy registered for '{resolvedMode}'.");
        }

        return strategy;
    }
}
