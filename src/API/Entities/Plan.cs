namespace Orchi.Api.Entities;

public class Plan
{
    public required string PlanId { get; set; }

    public Guid SourceChatId { get; set; }

    public required string Title { get; set; }

    public required string ContentMarkdown { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Chat SourceChat { get; set; } = null!;
}
