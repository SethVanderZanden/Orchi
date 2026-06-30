namespace Orchi.Api.Infrastructure.Agents.Modes.Plan;

public enum PlanStatus
{
    Draft,
    Ready,
    Dispatched,
    Completed,
    Superseded
}

public enum SubPlanStatus
{
    Draft,
    Ready,
    Dispatched,
    Completed
}

public sealed class SubPlan
{
    public required Guid Id { get; init; }

    public required string Title { get; set; }

    public required string ContentMarkdown { get; set; }

    public Guid? AssignedChatId { get; set; }

    public SubPlanStatus Status { get; set; } = SubPlanStatus.Draft;
}

public sealed class PlanArtifact
{
    public required Guid Id { get; init; }

    public required Guid SourceChatId { get; init; }

    public required string Title { get; set; }

    public required string ContentMarkdown { get; set; }

    public PlanStatus Status { get; set; } = PlanStatus.Draft;

    public List<SubPlan> SubPlans { get; } = [];
}
