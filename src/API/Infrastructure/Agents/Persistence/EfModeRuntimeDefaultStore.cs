using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed class EfModeRuntimeDefaultStore(IDbContextFactory<AppDbContext> dbContextFactory)
    : IModeRuntimeDefaultStore
{
    public async Task<IReadOnlyList<StoredModeRuntimeDefault>> ListAsync(CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<ModeRuntimeDefault> entities = await db.ModeRuntimeDefaults
            .AsNoTracking()
            .OrderBy(row => row.Mode)
            .ToListAsync(cancellationToken);

        return entities.Select(ToStored).ToArray();
    }

    public async Task<StoredModeRuntimeDefault?> GetAsync(string mode, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ModeRuntimeDefault? entity = await db.ModeRuntimeDefaults
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.Mode == mode, cancellationToken);

        return entity is null ? null : ToStored(entity);
    }

    public async Task<StoredModeRuntimeDefault> UpsertAsync(
        string mode,
        string agentId,
        string? modelId,
        string? contextSizeId,
        string? reasoningEffortId,
        string? approvalPolicyId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ModeRuntimeDefault? existing = await db.ModeRuntimeDefaults
            .FirstOrDefaultAsync(row => row.Mode == mode, cancellationToken);

        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            var entity = new ModeRuntimeDefault
            {
                Mode = mode,
                AgentId = agentId,
                ModelId = modelId,
                ContextSizeId = contextSizeId,
                ReasoningEffortId = reasoningEffortId,
                ApprovalPolicyId = approvalPolicyId,
                UpdatedAt = now
            };

            db.ModeRuntimeDefaults.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            return ToStored(entity);
        }

        existing.AgentId = agentId;
        existing.ModelId = modelId;
        existing.ContextSizeId = contextSizeId;
        existing.ReasoningEffortId = reasoningEffortId;
        existing.ApprovalPolicyId = approvalPolicyId;
        existing.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return ToStored(existing);
    }

    private static StoredModeRuntimeDefault ToStored(ModeRuntimeDefault entity) =>
        new(
            entity.Mode,
            entity.AgentId,
            entity.ModelId,
            entity.ContextSizeId,
            entity.ReasoningEffortId,
            entity.ApprovalPolicyId,
            entity.UpdatedAt);
}
