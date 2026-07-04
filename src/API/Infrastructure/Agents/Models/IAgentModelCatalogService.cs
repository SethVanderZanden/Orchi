using Orchi.Api.Common.Results;

namespace Orchi.Api.Infrastructure.Agents.Models;

public interface IAgentModelCatalogService
{
    Task<IReadOnlyList<AgentModelDto>> ListAsync(
        string agentId,
        bool includeDisabled,
        CancellationToken cancellationToken);

    Task<DateTimeOffset?> GetLastSyncedAtAsync(string agentId, CancellationToken cancellationToken);

    Task<Result<AgentModelSyncResult>> SyncAsync(string agentId, CancellationToken cancellationToken);

    Task<Result<AgentModelDto>> AddManualAsync(
        string agentId,
        string modelId,
        CancellationToken cancellationToken);

    Task<Result<AgentModelDto>> UpdateEnabledAsync(
        string agentId,
        string modelId,
        bool enabled,
        CancellationToken cancellationToken);

    Task<Result> RemoveManualAsync(
        string agentId,
        string modelId,
        CancellationToken cancellationToken);

    Task<bool> IsEnabledModelAsync(
        string agentId,
        string modelId,
        CancellationToken cancellationToken);
}

public sealed record AgentModelDto(
    string Id,
    string Label,
    bool IsDefault,
    bool IsCurrent,
    bool IsEnabled,
    string Source);

public sealed record AgentModelSyncResult(
    IReadOnlyList<AgentModelDto> Models,
    DateTimeOffset SyncedAt);
