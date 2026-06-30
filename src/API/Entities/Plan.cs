namespace Orchi.Api.Entities;

public class Plan
{
    public Guid Id { get; set; }

    public Guid SourceChatId { get; set; }

    public required string Title { get; set; }

    public required string ContentMarkdown { get; set; }

    public required string Status { get; set; }

    public List<SubPlan> SubPlans { get; set; } = [];
}
