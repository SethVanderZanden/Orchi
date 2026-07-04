namespace Orchi.Api.Infrastructure.Agents.Models;

public sealed class AgentModelListProviderFactory(IEnumerable<IAgentModelListProvider> providers)
{
    private readonly Dictionary<string, IAgentModelListProvider> _providers =
        providers.ToDictionary(provider => provider.AgentId, StringComparer.OrdinalIgnoreCase);

    public IAgentModelListProvider GetProvider(string agentId)
    {
        if (!_providers.TryGetValue(agentId, out IAgentModelListProvider? provider))
        {
            throw new InvalidOperationException($"No model list provider registered for agent '{agentId}'.");
        }

        return provider;
    }
}
