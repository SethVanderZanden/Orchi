namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed record StoredModeRuntimeDefault(
    string Mode,
    string AgentId,
    string? ModelId,
    string? ContextSizeId,
    string? ReasoningEffortId,
    string? ApprovalPolicyId,
    DateTimeOffset UpdatedAt);

public interface IModeRuntimeDefaultStore
{
    Task<IReadOnlyList<StoredModeRuntimeDefault>> ListAsync(CancellationToken cancellationToken);

    Task<StoredModeRuntimeDefault?> GetAsync(string mode, CancellationToken cancellationToken);

    Task<StoredModeRuntimeDefault> UpsertAsync(
        string mode,
        string agentId,
        string? modelId,
        string? contextSizeId,
        string? reasoningEffortId,
        string? approvalPolicyId,
        CancellationToken cancellationToken);
}
