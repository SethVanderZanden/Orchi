using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed class EfAgentModeModelDefaultStore(IDbContextFactory<AppDbContext> dbContextFactory)
    : IAgentModeModelDefaultStore
{
    public async Task<IReadOnlyList<StoredAgentModeModelDefault>> ListAsync(
        string agentId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<AgentModeModelDefault> entities = await db.AgentModeModelDefaults
            .AsNoTracking()
            .Where(row => row.AgentId == agentId)
            .OrderBy(row => row.Mode)
            .ToListAsync(cancellationToken);

        return entities.Select(ToStored).ToArray();
    }

    public async Task<StoredAgentModeModelDefault?> GetAsync(
        string agentId,
        string mode,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AgentModeModelDefault? entity = await db.AgentModeModelDefaults
            .AsNoTracking()
            .FirstOrDefaultAsync(
                row => row.AgentId == agentId && row.Mode == mode,
                cancellationToken);

        return entity is null ? null : ToStored(entity);
    }

    public async Task<StoredAgentModeModelDefault> UpsertAsync(
        string agentId,
        string mode,
        string? modelId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AgentModeModelDefault? existing = await db.AgentModeModelDefaults
            .FirstOrDefaultAsync(
                row => row.AgentId == agentId && row.Mode == mode,
                cancellationToken);

        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (existing is null)
        {
            var entity = new AgentModeModelDefault
            {
                AgentId = agentId,
                Mode = mode,
                ModelId = modelId,
                UpdatedAt = now
            };

            db.AgentModeModelDefaults.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            return ToStored(entity);
        }

        existing.ModelId = modelId;
        existing.UpdatedAt = now;
        await db.SaveChangesAsync(cancellationToken);
        return ToStored(existing);
    }

    private static StoredAgentModeModelDefault ToStored(AgentModeModelDefault entity) =>
        new(entity.AgentId, entity.Mode, entity.ModelId, entity.UpdatedAt);
}
