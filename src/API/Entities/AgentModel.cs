namespace Orchi.Api.Entities;

public class AgentModel
{
    public required string AgentId { get; set; }

    public required string ModelId { get; set; }

    public required string Label { get; set; }

    public bool IsEnabled { get; set; } = true;

    public bool IsDefault { get; set; }

    public bool IsCurrent { get; set; }

    public required string Source { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
