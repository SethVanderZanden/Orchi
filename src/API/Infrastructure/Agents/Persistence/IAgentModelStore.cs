using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed record StoredAgentModel(
    string AgentId,
    string ModelId,
    string Label,
    bool IsEnabled,
    bool IsDefault,
    bool IsCurrent,
    string Source,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public interface IAgentModelStore
{
    Task<IReadOnlyList<StoredAgentModel>> ListAsync(
        string agentId,
        bool includeDisabled,
        CancellationToken cancellationToken);

    Task<StoredAgentModel?> GetAsync(
        string agentId,
        string modelId,
        CancellationToken cancellationToken);

    Task<DateTimeOffset?> GetLastSyncedAtAsync(string agentId, CancellationToken cancellationToken);

    Task MergeCliModelsAsync(
        string agentId,
        IReadOnlyList<CliModelMergeEntry> entries,
        DateTimeOffset syncedAt,
        CancellationToken cancellationToken);

    Task<StoredAgentModel> AddManualAsync(
        string agentId,
        string modelId,
        string? label,
        CancellationToken cancellationToken);

    Task EnsureBuiltInAsync(
        string agentId,
        string modelId,
        string label,
        bool isDefault,
        CancellationToken cancellationToken);

    Task<StoredAgentModel?> UpdateEnabledAsync(
        string agentId,
        string modelId,
        bool isEnabled,
        CancellationToken cancellationToken);

    Task<bool> RemoveAsync(
        string agentId,
        string modelId,
        CancellationToken cancellationToken);
}

public sealed record CliModelMergeEntry(
    string ModelId,
    string Label,
    bool IsDefault,
    bool IsCurrent);
