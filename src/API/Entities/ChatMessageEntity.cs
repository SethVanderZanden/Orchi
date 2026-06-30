namespace Orchi.Api.Entities;

public class ChatMessageEntity
{
    public Guid Id { get; set; }

    public Guid ChatId { get; set; }

    public required string Role { get; set; }

    public required string Content { get; set; }

    public required string Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int Ordinal { get; set; }

    public Chat Chat { get; set; } = null!;
}
