namespace Orchi.Api.Entities;

public class Chat
{
    public Guid Id { get; set; }

    public required string AgentId { get; set; }

    public required string WorkspacePath { get; set; }

    public string Mode { get; set; } = "default";

    public Guid? ParentChatId { get; set; }

    public string? PlanFilePath { get; set; }

    public string? ExternalSessionId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public List<ChatMessageEntity> Messages { get; set; } = [];
}
