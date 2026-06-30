namespace Orchi.Api.Entities;

public class Chat
{
    public Guid Id { get; set; }

    public required string AgentId { get; set; }

    public required string WorkspacePath { get; set; }

    public required string Mode { get; set; }

    public Guid? ParentChatId { get; set; }

    public Guid? AttachedPlanId { get; set; }

    public Guid? GoalChatId { get; set; }

    public string? ExternalSessionId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public bool IsDeleted { get; set; }

    public List<ChatMessageEntity> Messages { get; set; } = [];

    public List<GoalJournalEntry> GoalJournal { get; set; } = [];
}
