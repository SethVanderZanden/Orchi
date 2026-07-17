namespace Orchi.Api.Entities;

public class AgentContextSize
{
    public required string AgentId { get; set; }

    public required string SizeId { get; set; }

    public required string Label { get; set; }

    public int TokenCount { get; set; }

    public bool IsEnabled { get; set; } = true;

    public required string Source { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
