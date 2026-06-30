using System.Collections.Concurrent;

namespace Orchi.Api.Infrastructure.Agents.Modes.Plan;

public sealed class InMemoryPlanStore : IPlanStore
{
    private readonly ConcurrentDictionary<Guid, PlanArtifact> _plans = new();

    public PlanArtifact Create(Guid sourceChatId, string title, string contentMarkdown)
    {
        var plan = new PlanArtifact
        {
            Id = Guid.NewGuid(),
            SourceChatId = sourceChatId,
            Title = title,
            ContentMarkdown = contentMarkdown
        };

        _plans[plan.Id] = plan;
        return plan;
    }

    public PlanArtifact? Get(Guid planId) =>
        _plans.TryGetValue(planId, out PlanArtifact? plan) ? plan : null;

    public SubPlan? GetSubPlan(Guid subPlanId)
    {
        foreach (PlanArtifact plan in _plans.Values)
        {
            SubPlan? subPlan = plan.SubPlans.FirstOrDefault(sub => sub.Id == subPlanId);
            if (subPlan is not null)
            {
                return subPlan;
            }
        }

        return null;
    }

    public IReadOnlyList<PlanArtifact> ListBySourceChat(Guid sourceChatId) =>
        _plans.Values
            .Where(plan => plan.SourceChatId == sourceChatId)
            .OrderByDescending(plan => plan.Id)
            .ToList();

    public bool TryResolveContent(Guid planOrSubPlanId, out string contentMarkdown, out string title)
    {
        if (_plans.TryGetValue(planOrSubPlanId, out PlanArtifact? plan))
        {
            contentMarkdown = plan.ContentMarkdown;
            title = plan.Title;
            return true;
        }

        SubPlan? subPlan = GetSubPlan(planOrSubPlanId);
        if (subPlan is not null)
        {
            contentMarkdown = subPlan.ContentMarkdown;
            title = subPlan.Title;
            return true;
        }

        contentMarkdown = string.Empty;
        title = string.Empty;
        return false;
    }

    public Guid? GetPlanIdForSubPlan(Guid subPlanId)
    {
        foreach (PlanArtifact plan in _plans.Values)
        {
            if (plan.SubPlans.Any(sub => sub.Id == subPlanId))
            {
                return plan.Id;
            }
        }

        return null;
    }

    public Task SavePlanAsync(PlanArtifact plan, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task SaveSubPlanAsync(Guid planId, SubPlan subPlan, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
