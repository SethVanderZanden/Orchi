namespace Orchi.Api.Infrastructure.Agents.Cli;

internal sealed class AgentCliProcessorFactory(IEnumerable<IAgentCliProcessorProfile> profiles) : IAgentCliProcessorFactory
{
    private readonly Dictionary<string, IAgentCliProcessorProfile> _profiles =
        profiles.ToDictionary(profile => profile.AgentId, StringComparer.OrdinalIgnoreCase);

    public IAgentCliProcessorProfile GetProfile(string agentId)
    {
        if (!_profiles.TryGetValue(agentId, out IAgentCliProcessorProfile? profile))
        {
            throw new InvalidOperationException($"No CLI processor profile registered for '{agentId}'.");
        }

        return profile;
    }
}
