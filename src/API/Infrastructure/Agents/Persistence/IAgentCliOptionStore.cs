namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed record StoredAgentCliOption(
    string AgentId,
    string Kind,
    string OptionId,
    string Label,
    string CliValue,
    bool IsEnabled,
    string Source,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public interface IAgentCliOptionStore
{
    Task<IReadOnlyList<StoredAgentCliOption>> ListAsync(
        string agentId,
        string kind,
        bool includeDisabled,
        CancellationToken cancellationToken);

    Task<StoredAgentCliOption?> GetAsync(
        string agentId,
        string kind,
        string optionId,
        CancellationToken cancellationToken);

    Task<StoredAgentCliOption> AddManualAsync(
        string agentId,
        string kind,
        string optionId,
        string label,
        string cliValue,
        CancellationToken cancellationToken);

    Task<StoredAgentCliOption?> UpdateEnabledAsync(
        string agentId,
        string kind,
        string optionId,
        bool isEnabled,
        CancellationToken cancellationToken);

    Task<bool> RemoveAsync(
        string agentId,
        string kind,
        string optionId,
        CancellationToken cancellationToken);
}
