namespace Orchi.Api.Infrastructure.Agents.Models;

/// <summary>
/// Known agent CLI config option kinds. Values match Codex <c>config.toml</c> / <c>-c</c> keys
/// from the advanced configuration docs.
/// </summary>
public static class AgentCliOptionKinds
{
    public const string ModelReasoningEffort = "model_reasoning_effort";

    public const string ApprovalPolicy = "approval_policy";

    public static readonly IReadOnlyList<string> All =
    [
        ModelReasoningEffort,
        ApprovalPolicy
    ];

    public static bool IsKnown(string kind) =>
        All.Contains(kind, StringComparer.OrdinalIgnoreCase);

    public static string Normalize(string kind)
    {
        string trimmed = kind.Trim();
        string? match = All.FirstOrDefault(
            candidate => string.Equals(candidate, trimmed, StringComparison.OrdinalIgnoreCase));

        return match ?? trimmed;
    }
}
