namespace Orchi.Api.Infrastructure.Agents;

internal sealed class AgentAdapterFactory(IEnumerable<IAgentAdapter> adapters) : IAgentAdapterFactory
{
    private readonly Dictionary<string, IAgentAdapter> _adapters =
        adapters.ToDictionary(adapter => adapter.AgentId, StringComparer.OrdinalIgnoreCase);

    public IAgentAdapter GetAdapter(string agentId)
    {
        if (!_adapters.TryGetValue(agentId, out IAgentAdapter? adapter))
        {
            throw new InvalidOperationException($"No agent adapter registered for '{agentId}'.");
        }

        return adapter;
    }

    public IReadOnlyList<string> ListAgentIds() => _adapters.Keys.ToArray();
}
