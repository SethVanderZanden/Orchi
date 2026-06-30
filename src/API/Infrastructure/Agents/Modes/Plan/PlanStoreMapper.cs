using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;
using DomainSubPlan = Orchi.Api.Infrastructure.Agents.Modes.Plan.SubPlan;

namespace Orchi.Api.Infrastructure.Agents.Modes.Plan;

internal static class PlanStoreMapper
{
    public static PlanArtifact ToArtifact(Entities.Plan entity)
    {
        var artifact = new PlanArtifact
        {
            Id = entity.Id,
            SourceChatId = entity.SourceChatId,
            Title = entity.Title,
            ContentMarkdown = entity.ContentMarkdown,
            Status = Enum.TryParse<PlanStatus>(entity.Status, ignoreCase: true, out PlanStatus status)
                ? status
                : PlanStatus.Draft
        };

        foreach (Entities.SubPlan subPlan in entity.SubPlans)
        {
            artifact.SubPlans.Add(ToDomainSubPlan(subPlan));
        }

        return artifact;
    }

    public static DomainSubPlan ToDomainSubPlan(Entities.SubPlan entity) =>
        new()
        {
            Id = entity.Id,
            Title = entity.Title,
            ContentMarkdown = entity.ContentMarkdown,
            AssignedChatId = entity.AssignedChatId,
            Status = Enum.TryParse<SubPlanStatus>(entity.Status, ignoreCase: true, out SubPlanStatus status)
                ? status
                : SubPlanStatus.Draft
        };

    public static Entities.Plan ToEntity(PlanArtifact artifact) =>
        new()
        {
            Id = artifact.Id,
            SourceChatId = artifact.SourceChatId,
            Title = artifact.Title,
            ContentMarkdown = artifact.ContentMarkdown,
            Status = artifact.Status.ToString(),
            SubPlans = artifact.SubPlans.Select(subPlan => ToEntity(subPlan, artifact.Id)).ToList()
        };

    public static Entities.SubPlan ToEntity(DomainSubPlan subPlan, Guid planId) =>
        new()
        {
            Id = subPlan.Id,
            PlanId = planId,
            Title = subPlan.Title,
            ContentMarkdown = subPlan.ContentMarkdown,
            AssignedChatId = subPlan.AssignedChatId,
            Status = subPlan.Status.ToString()
        };
}
