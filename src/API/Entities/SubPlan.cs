namespace Orchi.Api.Entities;

public class SubPlan
{
    public Guid Id { get; set; }

    public Guid PlanId { get; set; }

    public required string Title { get; set; }

    public required string ContentMarkdown { get; set; }

    public Guid? AssignedChatId { get; set; }

    public required string Status { get; set; }

    public Plan Plan { get; set; } = null!;
}
