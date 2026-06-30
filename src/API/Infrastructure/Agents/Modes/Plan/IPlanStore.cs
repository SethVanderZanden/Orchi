namespace Orchi.Api.Infrastructure.Agents.Modes.Plan;

public interface IPlanStore
{
    PlanArtifact Create(Guid sourceChatId, string title, string contentMarkdown);

    PlanArtifact? Get(Guid planId);

    SubPlan? GetSubPlan(Guid subPlanId);

    IReadOnlyList<PlanArtifact> ListBySourceChat(Guid sourceChatId);

    bool TryResolveContent(Guid planOrSubPlanId, out string contentMarkdown, out string title);

    Guid? GetPlanIdForSubPlan(Guid subPlanId);

    Task SavePlanAsync(PlanArtifact plan, CancellationToken cancellationToken);

    Task SaveSubPlanAsync(Guid planId, SubPlan subPlan, CancellationToken cancellationToken);
}
