namespace Orchi.Api.Entities;

public class ModeRuntimeDefault
{
    public required string Mode { get; set; }

    public required string AgentId { get; set; }

    public string? ModelId { get; set; }

    public string? ContextSizeId { get; set; }

    public string? ReasoningEffortId { get; set; }

    public string? ApprovalPolicyId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
