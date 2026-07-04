using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed class EfAgentModelStore(IDbContextFactory<AppDbContext> dbContextFactory) : IAgentModelStore
{
    public async Task<IReadOnlyList<StoredAgentModel>> ListAsync(
        string agentId,
        bool includeDisabled,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<AgentModel> query = db.AgentModels
            .AsNoTracking()
            .Where(model => model.AgentId == agentId);

        if (!includeDisabled)
        {
            query = query.Where(model => model.IsEnabled);
        }

        List<AgentModel> entities = await query
            .OrderBy(model => model.Label)
            .ToListAsync(cancellationToken);

        return entities.Select(ToStored).ToArray();
    }

    public async Task<StoredAgentModel?> GetAsync(
        string agentId,
        string modelId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AgentModel? entity = await db.AgentModels
            .AsNoTracking()
            .FirstOrDefaultAsync(
                model => model.AgentId == agentId && model.ModelId == modelId,
                cancellationToken);

        return entity is null ? null : ToStored(entity);
    }

    public async Task<DateTimeOffset?> GetLastSyncedAtAsync(string agentId, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<DateTimeOffset> syncTimes = await db.AgentModels
            .AsNoTracking()
            .Where(model => model.AgentId == agentId && model.Source == AgentModelSource.Cli)
            .Select(model => model.UpdatedAt)
            .ToListAsync(cancellationToken);

        return syncTimes.Count == 0 ? null : syncTimes.Max();
    }

    public async Task MergeCliModelsAsync(
        string agentId,
        IReadOnlyList<CliModelMergeEntry> entries,
        DateTimeOffset syncedAt,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<AgentModel> existing = await db.AgentModels
            .Where(model => model.AgentId == agentId)
            .ToListAsync(cancellationToken);

        var syncedIds = new HashSet<string>(
            entries.Select(entry => entry.ModelId),
            StringComparer.OrdinalIgnoreCase);

        foreach (CliModelMergeEntry entry in entries)
        {
            AgentModel? row = existing.FirstOrDefault(
                model => string.Equals(model.ModelId, entry.ModelId, StringComparison.OrdinalIgnoreCase));

            if (row is null)
            {
                db.AgentModels.Add(new AgentModel
                {
                    AgentId = agentId,
                    ModelId = entry.ModelId,
                    Label = entry.Label,
                    IsEnabled = true,
                    IsDefault = entry.IsDefault,
                    IsCurrent = entry.IsCurrent,
                    Source = AgentModelSource.Cli,
                    CreatedAt = syncedAt,
                    UpdatedAt = syncedAt
                });
                continue;
            }

            row.Label = entry.Label;
            row.IsDefault = entry.IsDefault;
            row.IsCurrent = entry.IsCurrent;
            row.UpdatedAt = syncedAt;

            if (string.Equals(row.Source, AgentModelSource.Cli, StringComparison.OrdinalIgnoreCase))
            {
                row.IsEnabled = true;
            }
        }

        foreach (AgentModel row in existing)
        {
            if (!string.Equals(row.Source, AgentModelSource.Cli, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (syncedIds.Contains(row.ModelId))
            {
                continue;
            }

            row.IsEnabled = false;
            row.IsDefault = false;
            row.IsCurrent = false;
            row.UpdatedAt = syncedAt;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<StoredAgentModel> AddManualAsync(
        string agentId,
        string modelId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AgentModel? existing = await db.AgentModels
            .FirstOrDefaultAsync(
                model => model.AgentId == agentId && model.ModelId == modelId,
                cancellationToken);

        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (existing is not null)
        {
            if (string.Equals(existing.Source, AgentModelSource.Manual, StringComparison.OrdinalIgnoreCase))
            {
                return ToStored(existing);
            }

            existing.Source = AgentModelSource.Manual;
            existing.IsEnabled = true;
            existing.UpdatedAt = now;
            await db.SaveChangesAsync(cancellationToken);
            return ToStored(existing);
        }

        var entity = new AgentModel
        {
            AgentId = agentId,
            ModelId = modelId,
            Label = modelId,
            IsEnabled = true,
            Source = AgentModelSource.Manual,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.AgentModels.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToStored(entity);
    }

    public async Task<StoredAgentModel?> UpdateEnabledAsync(
        string agentId,
        string modelId,
        bool isEnabled,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AgentModel? entity = await db.AgentModels
            .FirstOrDefaultAsync(
                model => model.AgentId == agentId && model.ModelId == modelId,
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

    public async Task<bool> RemoveManualAsync(
        string agentId,
        string modelId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        AgentModel? entity = await db.AgentModels
            .FirstOrDefaultAsync(
                model => model.AgentId == agentId && model.ModelId == modelId,
                cancellationToken);

        if (entity is null
            || !string.Equals(entity.Source, AgentModelSource.Manual, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        db.AgentModels.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static StoredAgentModel ToStored(AgentModel entity) =>
        new(
            entity.AgentId,
            entity.ModelId,
            entity.Label,
            entity.IsEnabled,
            entity.IsDefault,
            entity.IsCurrent,
            entity.Source,
            entity.CreatedAt,
            entity.UpdatedAt);
}
