namespace Orchi.Api.Entities;

/// <summary>
/// User-curated CLI config option for an agent (e.g. Codex <c>model_reasoning_effort</c>, <c>approval_policy</c>).
/// Adapters emit selected values via agent-specific CLI overrides (Codex: <c>-c key=value</c>).
/// </summary>
public class AgentCliOption
{
    public required string AgentId { get; set; }

    /// <summary>Config key / kind, e.g. <c>model_reasoning_effort</c> or <c>approval_policy</c>.</summary>
    public required string Kind { get; set; }

    public required string OptionId { get; set; }

    public required string Label { get; set; }

    /// <summary>Raw value passed to the CLI config override (usually matches <see cref="OptionId"/>).</summary>
    public required string CliValue { get; set; }

    public bool IsEnabled { get; set; } = true;

    public required string Source { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
