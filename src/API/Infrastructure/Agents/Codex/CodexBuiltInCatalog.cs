using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal static class CodexBuiltInCatalog
{
    internal sealed record CliOptionPreset(string Kind, string OptionId, string Label, string CliValue);

    internal sealed record ModelPreset(string ModelId, string Label, bool IsDefault = false);

    /// <summary>
    /// GPT-5.6 capability tiers as shown in Codex (Sol / Terra / Luna).
    /// Model id is what Orchi passes to <c>codex exec --model</c>; label is the UI name.
    /// </summary>
    internal static IReadOnlyList<ModelPreset> ModelPresets { get; } =
    [
        new("gpt-5.6-sol", "5.6 Sol"),
        new("gpt-5.6-terra", "5.6 Terra", IsDefault: true),
        new("gpt-5.6-luna", "5.6 Luna"),
        new("gpt-5.6", "5.6 (Sol alias)")
    ];

    internal static IReadOnlyList<CliOptionPreset> AllCliOptionPresets { get; } =
    [
        new(AgentCliOptionKinds.ModelReasoningEffort, "none", "None", "none"),
        new(AgentCliOptionKinds.ModelReasoningEffort, "minimal", "Minimal", "minimal"),
        new(AgentCliOptionKinds.ModelReasoningEffort, "low", "Low", "low"),
        new(AgentCliOptionKinds.ModelReasoningEffort, "medium", "Medium", "medium"),
        new(AgentCliOptionKinds.ModelReasoningEffort, "high", "High", "high"),
        new(AgentCliOptionKinds.ModelReasoningEffort, "xhigh", "Extra high", "xhigh"),
        new(AgentCliOptionKinds.ModelReasoningEffort, "max", "Max", "max"),
        new(AgentCliOptionKinds.ApprovalPolicy, "untrusted", "Untrusted", "untrusted"),
        new(AgentCliOptionKinds.ApprovalPolicy, "on-request", "On request", "on-request"),
        new(AgentCliOptionKinds.ApprovalPolicy, "never", "Never (automatic)", "never")
    ];

    /// <summary>Backward-compatible alias used by existing call sites. </summary>
    internal static IReadOnlyList<CliOptionPreset> AllPresets => AllCliOptionPresets;

    internal const string DefaultApprovalPolicyId = "never";

    internal const string DefaultReasoningEffortId = "medium";

    internal const string DefaultModelId = "gpt-5.6-terra";
}
