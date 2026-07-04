namespace Orchi.Api.Infrastructure.Agents.Models;

public sealed record AgentModelListEntry(
    string ModelId,
    string Label,
    bool IsDefault,
    bool IsCurrent);

public interface IAgentModelListProvider
{
    string AgentId { get; }

    Task<IReadOnlyList<AgentModelListEntry>> FetchModelsAsync(CancellationToken cancellationToken);
}
