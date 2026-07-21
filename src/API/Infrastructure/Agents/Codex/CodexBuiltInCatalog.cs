using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal static class CodexBuiltInCatalog
{
    internal sealed record Preset(string Kind, string OptionId, string Label, string CliValue);

    internal static IReadOnlyList<Preset> AllPresets { get; } =
    [
        new(AgentCliOptionKinds.ModelReasoningEffort, "minimal", "Minimal", "minimal"),
        new(AgentCliOptionKinds.ModelReasoningEffort, "low", "Low", "low"),
        new(AgentCliOptionKinds.ModelReasoningEffort, "medium", "Medium", "medium"),
        new(AgentCliOptionKinds.ModelReasoningEffort, "high", "High", "high"),
        new(AgentCliOptionKinds.ModelReasoningEffort, "xhigh", "Extra high", "xhigh"),
        new(AgentCliOptionKinds.ApprovalPolicy, "untrusted", "Untrusted", "untrusted"),
        new(AgentCliOptionKinds.ApprovalPolicy, "on-request", "On request", "on-request"),
        new(AgentCliOptionKinds.ApprovalPolicy, "never", "Never (automatic)", "never")
    ];

    internal const string DefaultApprovalPolicyId = "never";
}
