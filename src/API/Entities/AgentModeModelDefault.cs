namespace Orchi.Api.Entities;

public class AgentModeModelDefault
{
    public required string AgentId { get; set; }

    public required string Mode { get; set; }

    public string? ModelId { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
