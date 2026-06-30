using Orchi.Api.Common.Results;

namespace Orchi.Api.Infrastructure.Agents.Modes.Plan;

public sealed class PlanManager(IPlanStore store)
{
    public Result<PlanArtifact> CreatePlan(Guid sourceChatId, string title, string contentMarkdown)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return Result.Failure<PlanArtifact>(Error.Validation("Plan.TitleRequired", "Plan title is required."));
        }

        if (string.IsNullOrWhiteSpace(contentMarkdown))
        {
            return Result.Failure<PlanArtifact>(Error.Validation("Plan.ContentRequired", "Plan content is required."));
        }

        return Result.Success(store.Create(sourceChatId, title.Trim(), contentMarkdown.Trim()));
    }

    public async Task<Result<PlanArtifact>> UpdatePlanAsync(
        Guid planId,
        string? title,
        string? contentMarkdown,
        PlanStatus? status,
        CancellationToken cancellationToken)
    {
        PlanArtifact? plan = store.Get(planId);
        if (plan is null)
        {
            return Result.Failure<PlanArtifact>(Error.NotFound($"Plan '{planId}' was not found."));
        }

        if (!string.IsNullOrWhiteSpace(title))
        {
            plan.Title = title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(contentMarkdown))
        {
            plan.ContentMarkdown = contentMarkdown.Trim();
        }

        if (status is not null)
        {
            plan.Status = status.Value;
        }

        await store.SavePlanAsync(plan, cancellationToken);
        return Result.Success(plan);
    }

    public async Task<Result<PlanArtifact>> UpsertSubPlansAsync(
        Guid planId,
        IReadOnlyList<SubPlanInput> subPlans,
        CancellationToken cancellationToken)
    {
        PlanArtifact? plan = store.Get(planId);
        if (plan is null)
        {
            return Result.Failure<PlanArtifact>(Error.NotFound($"Plan '{planId}' was not found."));
        }

        foreach (SubPlanInput input in subPlans)
        {
            SubPlan? existing = plan.SubPlans.FirstOrDefault(sub => sub.Id == input.Id);
            if (existing is not null)
            {
                existing.Title = input.Title;
                existing.ContentMarkdown = input.ContentMarkdown;
                await store.SaveSubPlanAsync(planId, existing, cancellationToken);
                continue;
            }

            var subPlan = new SubPlan
            {
                Id = input.Id == Guid.Empty ? Guid.NewGuid() : input.Id,
                Title = input.Title,
                ContentMarkdown = input.ContentMarkdown,
                Status = SubPlanStatus.Ready
            };

            plan.SubPlans.Add(subPlan);
            await store.SaveSubPlanAsync(planId, subPlan, cancellationToken);
        }

        return Result.Success(plan);
    }

    public Result ValidateAttachedPlan(Guid? attachedPlanId)
    {
        if (attachedPlanId is null)
        {
            return Result.Failure(Error.Validation("Plan.Required", "Implement mode requires an attached plan."));
        }

        if (!store.TryResolveContent(attachedPlanId.Value, out _, out _))
        {
            return Result.Failure(Error.Validation("Plan.NotFound", $"Attached plan '{attachedPlanId}' was not found."));
        }

        return Result.Success();
    }

    public Result<string> ResolvePlanContent(Guid planOrSubPlanId)
    {
        if (!store.TryResolveContent(planOrSubPlanId, out string content, out _))
        {
            return Result.Failure<string>(Error.NotFound($"Plan '{planOrSubPlanId}' was not found."));
        }

        return Result.Success(content);
    }

    public async Task MarkSubPlanDispatchedAsync(Guid subPlanId, Guid childChatId, CancellationToken cancellationToken)
    {
        SubPlan? subPlan = store.GetSubPlan(subPlanId);
        if (subPlan is null)
        {
            return;
        }

        subPlan.AssignedChatId = childChatId;
        subPlan.Status = SubPlanStatus.Dispatched;

        Guid? planId = store.GetPlanIdForSubPlan(subPlanId);
        if (planId is not null)
        {
            await store.SaveSubPlanAsync(planId.Value, subPlan, cancellationToken);
        }
    }

    public Task SavePlanAsync(PlanArtifact plan, CancellationToken cancellationToken) =>
        store.SavePlanAsync(plan, cancellationToken);
}

public sealed record SubPlanInput(Guid Id, string Title, string ContentMarkdown);
