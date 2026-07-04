using Orchi.Api.Common.Results;

namespace Orchi.Api.Infrastructure.Agents.Models;

public interface IAgentModeModelDefaultService
{
    Task<IReadOnlyList<AgentModeModelDefaultDto>> ListAsync(string agentId, CancellationToken cancellationToken);

    Task<Result<AgentModeModelDefaultDto>> UpdateAsync(
        string agentId,
        string mode,
        string? modelId,
        CancellationToken cancellationToken);

    Task<string?> ResolveAsync(string agentId, string mode, CancellationToken cancellationToken);
}

public sealed record AgentModeModelDefaultDto(string Mode, string Label, string? ModelId);
