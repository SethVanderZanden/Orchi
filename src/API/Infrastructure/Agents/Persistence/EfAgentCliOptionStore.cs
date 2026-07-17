using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed class EfAgentCliOptionStore(IDbContextFactory<AppDbContext> dbContextFactory)
    : IAgentCliOptionStore
{
    public async Task<IReadOnlyList<StoredAgentCliOption>> ListAsync(
        string agentId,
        string kind,
        bool includeDisabled,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<AgentCliOption> query = db.AgentCliOptions
            .AsNoTracking()
            .Where(option => option.AgentId == agentId && option.Kind == kind);

        if (!includeDisabled)
        {
            query = query.Where(option => option.IsEnabled);
        }

        List<AgentCliOption> entities = await query
            .OrderBy(option => option.Label)
            .ThenBy(option => option.OptionId)
            .ToListAsync(cancellationToken);

        return entities.Select(ToStored).ToArray();
    }

    public async Task<StoredAgentCliOption?> GetAsync(
        string agentId,
        string kind,
        string optionId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AgentCliOption? entity = await db.AgentCliOptions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                option => option.AgentId == agentId
                    && option.Kind == kind
                    && option.OptionId == optionId,
                cancellationToken);

        return entity is null ? null : ToStored(entity);
    }

    public async Task<StoredAgentCliOption> AddManualAsync(
        string agentId,
        string kind,
        string optionId,
        string label,
        string cliValue,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var entity = new AgentCliOption
        {
            AgentId = agentId,
            Kind = kind,
            OptionId = optionId,
            Label = label,
            CliValue = cliValue,
            IsEnabled = true,
            Source = AgentCliOptionSource.Manual,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.AgentCliOptions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToStored(entity);
    }

    public async Task<StoredAgentCliOption?> UpdateEnabledAsync(
        string agentId,
        string kind,
        string optionId,
        bool isEnabled,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AgentCliOption? entity = await db.AgentCliOptions
            .FirstOrDefaultAsync(
                option => option.AgentId == agentId
                    && option.Kind == kind
                    && option.OptionId == optionId,
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
        string kind,
        string optionId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AgentCliOption? entity = await db.AgentCliOptions
            .FirstOrDefaultAsync(
                option => option.AgentId == agentId
                    && option.Kind == kind
                    && option.OptionId == optionId,
                cancellationToken);

        if (entity is null)
        {
            return false;
        }

        db.AgentCliOptions.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static StoredAgentCliOption ToStored(AgentCliOption entity) =>
        new(
            entity.AgentId,
            entity.Kind,
            entity.OptionId,
            entity.Label,
            entity.CliValue,
            entity.IsEnabled,
            entity.Source,
            entity.CreatedAt,
            entity.UpdatedAt);
}
