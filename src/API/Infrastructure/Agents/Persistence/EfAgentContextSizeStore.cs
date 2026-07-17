using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed class EfAgentContextSizeStore(IDbContextFactory<AppDbContext> dbContextFactory)
    : IAgentContextSizeStore
{
    public async Task<IReadOnlyList<StoredAgentContextSize>> ListAsync(
        string agentId,
        bool includeDisabled,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<AgentContextSize> query = db.AgentContextSizes
            .AsNoTracking()
            .Where(size => size.AgentId == agentId);

        if (!includeDisabled)
        {
            query = query.Where(size => size.IsEnabled);
        }

        List<AgentContextSize> entities = await query
            .OrderBy(size => size.TokenCount)
            .ThenBy(size => size.Label)
            .ToListAsync(cancellationToken);

        return entities.Select(ToStored).ToArray();
    }

    public async Task<StoredAgentContextSize?> GetAsync(
        string agentId,
        string sizeId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AgentContextSize? entity = await db.AgentContextSizes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                size => size.AgentId == agentId && size.SizeId == sizeId,
                cancellationToken);

        return entity is null ? null : ToStored(entity);
    }

    public async Task<StoredAgentContextSize> AddManualAsync(
        string agentId,
        string sizeId,
        string label,
        int tokenCount,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var entity = new AgentContextSize
        {
            AgentId = agentId,
            SizeId = sizeId,
            Label = label,
            TokenCount = tokenCount,
            IsEnabled = true,
            Source = AgentContextSizeSource.Manual,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.AgentContextSizes.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToStored(entity);
    }

    public async Task<StoredAgentContextSize?> UpdateEnabledAsync(
        string agentId,
        string sizeId,
        bool isEnabled,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AgentContextSize? entity = await db.AgentContextSizes
            .FirstOrDefaultAsync(
                size => size.AgentId == agentId && size.SizeId == sizeId,
                cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.IsEnabled = isEnabled;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToStored(entity);
    }

    public async Task<bool> RemoveAsync(
        string agentId,
        string sizeId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AgentContextSize? entity = await db.AgentContextSizes
            .FirstOrDefaultAsync(
                size => size.AgentId == agentId && size.SizeId == sizeId,
                cancellationToken);

        if (entity is null)
        {
            return false;
        }

        db.AgentContextSizes.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static StoredAgentContextSize ToStored(AgentContextSize entity) =>
        new(
            entity.AgentId,
            entity.SizeId,
            entity.Label,
            entity.TokenCount,
            entity.IsEnabled,
            entity.Source,
            entity.CreatedAt,
            entity.UpdatedAt);
}
