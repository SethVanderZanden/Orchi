namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed record StoredAgentModeModelDefault(
    string AgentId,
    string Mode,
    string? ModelId,
    DateTimeOffset UpdatedAt);

public interface IAgentModeModelDefaultStore
{
    Task<IReadOnlyList<StoredAgentModeModelDefault>> ListAsync(
        string agentId,
        CancellationToken cancellationToken);

    Task<StoredAgentModeModelDefault?> GetAsync(
        string agentId,
        string mode,
        CancellationToken cancellationToken);

    Task<StoredAgentModeModelDefault> UpsertAsync(
        string agentId,
        string mode,
        string? modelId,
        CancellationToken cancellationToken);
}
