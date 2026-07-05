namespace Orchi.Api.Entities;

public class OrchestrationWorkflow
{
    public Guid ParentChatId { get; set; }

    public required string Status { get; set; }

    public string SequencePlanIdsJson { get; set; } = "[]";

    public int NextSequenceIndex { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Chat? ParentChat { get; set; }
}
