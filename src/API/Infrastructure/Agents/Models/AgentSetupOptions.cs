namespace Orchi.Api.Infrastructure.Agents.Models;

/// <summary>
/// Optional Codex preferences applied during first-time agent setup.
/// </summary>
public sealed record AgentSetupOptions(
    string? CodexApprovalPolicyId = null,
    string? CodexReasoningEffortId = null);
