using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Infrastructure.Agents.Codex;

/// <summary>
/// Codex has no stable public list-models CLI for Orchi sync; manual catalog entries are primary.
/// </summary>
internal sealed class CodexAgentModelListProvider : IAgentModelListProvider
{
    public string AgentId => AgentIds.Codex;

    public Task<IReadOnlyList<AgentModelListEntry>> FetchModelsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AgentModelListEntry>>([]);
}
