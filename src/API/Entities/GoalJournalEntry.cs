namespace Orchi.Api.Entities;

public class GoalJournalEntry
{
    public Guid Id { get; set; }

    public Guid ChatId { get; set; }

    public required string Content { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Chat Chat { get; set; } = null!;
}
