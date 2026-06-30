using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;

namespace Orchi.Api.Features.Chats.Shared;

public static class PlanMapper
{
    public static PlanResponse ToResponse(PlanArtifact plan) =>
        new(
            plan.Id,
            plan.SourceChatId,
            plan.Title,
            plan.ContentMarkdown,
            plan.Status.ToString(),
            plan.SubPlans.Select(sub => new SubPlanResponse(
                sub.Id,
                sub.Title,
                sub.ContentMarkdown,
                sub.AssignedChatId,
                sub.Status.ToString())).ToArray());
}
