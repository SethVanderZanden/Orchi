namespace Orchi.Api.Entities;

public class ChatMessageAttachment
{
    public Guid Id { get; set; }

    public Guid ChatId { get; set; }

    public Guid? MessageId { get; set; }

    public required string FileName { get; set; }

    public required string ContentType { get; set; }

    public long SizeBytes { get; set; }

    public required string WorkspaceRelativePath { get; set; }

    public string? ExtractedText { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public int Ordinal { get; set; }

    public Chat Chat { get; set; } = null!;

    public ChatMessageEntity? Message { get; set; }
}
