using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using DomainSubPlan = Orchi.Api.Infrastructure.Agents.Modes.Plan.SubPlan;

namespace Orchi.Api.Infrastructure.Agents.Modes.Plan;

public sealed class EfPlanStore(IDbContextFactory<AppDbContext> dbContextFactory) : IPlanStore
{
    public PlanArtifact Create(Guid sourceChatId, string title, string contentMarkdown)
    {
        using AppDbContext db = dbContextFactory.CreateDbContext();
        var entity = new Entities.Plan
        {
            Id = Guid.NewGuid(),
            SourceChatId = sourceChatId,
            Title = title,
            ContentMarkdown = contentMarkdown,
            Status = PlanStatus.Draft.ToString()
        };

        db.Plans.Add(entity);
        db.SaveChanges();
        return PlanStoreMapper.ToArtifact(entity);
    }

    public PlanArtifact? Get(Guid planId)
    {
        using AppDbContext db = dbContextFactory.CreateDbContext();
        Entities.Plan? entity = db.Plans
            .AsNoTracking()
            .Include(plan => plan.SubPlans)
            .FirstOrDefault(plan => plan.Id == planId);

        return entity is null ? null : PlanStoreMapper.ToArtifact(entity);
    }

    public DomainSubPlan? GetSubPlan(Guid subPlanId)
    {
        using AppDbContext db = dbContextFactory.CreateDbContext();
        Entities.SubPlan? entity = db.SubPlans.AsNoTracking().FirstOrDefault(sub => sub.Id == subPlanId);
        return entity is null ? null : PlanStoreMapper.ToDomainSubPlan(entity);
    }

    public IReadOnlyList<PlanArtifact> ListBySourceChat(Guid sourceChatId)
    {
        using AppDbContext db = dbContextFactory.CreateDbContext();
        return db.Plans
            .AsNoTracking()
            .Include(plan => plan.SubPlans)
            .Where(plan => plan.SourceChatId == sourceChatId)
            .OrderByDescending(plan => plan.Id)
            .AsEnumerable()
            .Select(PlanStoreMapper.ToArtifact)
            .ToArray();
    }

    public bool TryResolveContent(Guid planOrSubPlanId, out string contentMarkdown, out string title)
    {
        using AppDbContext db = dbContextFactory.CreateDbContext();
        Entities.Plan? plan = db.Plans.AsNoTracking().FirstOrDefault(existing => existing.Id == planOrSubPlanId);
        if (plan is not null)
        {
            contentMarkdown = plan.ContentMarkdown;
            title = plan.Title;
            return true;
        }

        Entities.SubPlan? subPlan = db.SubPlans.AsNoTracking().FirstOrDefault(existing => existing.Id == planOrSubPlanId);
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
        using AppDbContext db = dbContextFactory.CreateDbContext();
        return db.SubPlans.AsNoTracking()
            .Where(sub => sub.Id == subPlanId)
            .Select(sub => (Guid?)sub.PlanId)
            .FirstOrDefault();
    }

    public async Task SavePlanAsync(PlanArtifact plan, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Entities.Plan? entity = await db.Plans.FirstOrDefaultAsync(existing => existing.Id == plan.Id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        entity.Title = plan.Title;
        entity.ContentMarkdown = plan.ContentMarkdown;
        entity.Status = plan.Status.ToString();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveSubPlanAsync(Guid planId, DomainSubPlan subPlan, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Entities.SubPlan? entity = await db.SubPlans
            .FirstOrDefaultAsync(existing => existing.Id == subPlan.Id, cancellationToken);

        if (entity is null)
        {
            db.SubPlans.Add(PlanStoreMapper.ToEntity(subPlan, planId));
        }
        else
        {
            entity.Title = subPlan.Title;
            entity.ContentMarkdown = subPlan.ContentMarkdown;
            entity.AssignedChatId = subPlan.AssignedChatId;
            entity.Status = subPlan.Status.ToString();
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
