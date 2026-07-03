using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

namespace Orchi.Api.Infrastructure.Agents.Plans.Persistence;

public sealed class EfPlanStore(IDbContextFactory<AppDbContext> dbContextFactory) : IPlanStore
{
    public async Task UpsertAsync(PlanUpsertModel model, CancellationToken cancellationToken)
    {
        string planId = OrchiArtifactFileStore.SanitizePlanId(model.PlanId);
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Plan? existing = await db.Plans
            .FirstOrDefaultAsync(
                plan => plan.PlanId == planId && plan.SourceChatId == model.SourceChatId,
                cancellationToken);

        if (existing is null)
        {
            db.Plans.Add(new Plan
            {
                PlanId = planId,
                SourceChatId = model.SourceChatId,
                Title = model.Title,
                ContentMarkdown = model.ContentMarkdown,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            existing.Title = model.Title;
            existing.ContentMarkdown = model.ContentMarkdown;
            existing.UpdatedAt = now;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<StoredPlan?> GetAsync(
        Guid sourceChatId,
        string planId,
        CancellationToken cancellationToken)
    {
        string normalizedPlanId = OrchiArtifactFileStore.SanitizePlanId(planId);
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Plan? entity = await db.Plans
            .AsNoTracking()
            .FirstOrDefaultAsync(
                plan => plan.PlanId == normalizedPlanId && plan.SourceChatId == sourceChatId,
                cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return new StoredPlan(
            entity.PlanId,
            entity.SourceChatId,
            entity.Title,
            entity.ContentMarkdown,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
