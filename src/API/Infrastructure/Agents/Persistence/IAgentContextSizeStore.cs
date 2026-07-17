namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed record StoredAgentContextSize(
    string AgentId,
    string SizeId,
    string Label,
    int TokenCount,
    bool IsEnabled,
    string Source,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public interface IAgentContextSizeStore
{
    Task<IReadOnlyList<StoredAgentContextSize>> ListAsync(
        string agentId,
        bool includeDisabled,
        CancellationToken cancellationToken);

    Task<StoredAgentContextSize?> GetAsync(
        string agentId,
        string sizeId,
        CancellationToken cancellationToken);

    Task<StoredAgentContextSize> AddManualAsync(
        string agentId,
        string sizeId,
        string label,
        int tokenCount,
        CancellationToken cancellationToken);

    Task<StoredAgentContextSize?> UpdateEnabledAsync(
        string agentId,
        string sizeId,
        bool isEnabled,
        CancellationToken cancellationToken);

    Task<bool> RemoveAsync(
        string agentId,
        string sizeId,
        CancellationToken cancellationToken);
}
