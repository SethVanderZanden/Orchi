using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Scripts;

public sealed class EfScriptStore(IDbContextFactory<AppDbContext> dbContextFactory) : IScriptStore
{
    public async Task<IReadOnlyList<StoredScript>> ListAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Script> query = db.Scripts
            .AsNoTracking()
            .Include(script => script.Bindings);

        query = projectId is null
            ? query.Where(script => script.ProjectId == null)
            : query.Where(script => script.ProjectId == null || script.ProjectId == projectId);

        List<Script> entities = await query.ToListAsync(cancellationToken);

        return entities
            .OrderBy(script => script.ProjectId.HasValue ? 1 : 0)
            .ThenBy(script => script.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToStored)
            .ToArray();
    }

    public async Task<StoredScript?> GetAsync(string id, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Script? entity = await db.Scripts
            .AsNoTracking()
            .Include(script => script.Bindings)
            .FirstOrDefaultAsync(script => script.Id == id, cancellationToken);

        return entity is null ? null : ToStored(entity);
    }

    public async Task<StoredScript> CreateAsync(
        string name,
        Guid? projectId,
        string stepsJson,
        IReadOnlyList<ScriptUpsertBinding> bindings,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (projectId is not null)
        {
            bool projectExists = await db.Projects.AnyAsync(project => project.Id == projectId, cancellationToken);
            if (!projectExists)
            {
                throw new InvalidOperationException($"Project '{projectId}' was not found.");
            }
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var entity = new Script
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name.Trim(),
            ProjectId = projectId,
            StepsJson = stepsJson.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        foreach (ScriptUpsertBinding binding in bindings)
        {
            entity.Bindings.Add(CreateBindingEntity(entity.Id, binding));
        }

        db.Scripts.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToStored(entity);
    }

    public async Task<StoredScript?> UpdateAsync(
        string id,
        string name,
        string stepsJson,
        IReadOnlyList<ScriptUpsertBinding> bindings,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Script? entity = await db.Scripts
            .Include(script => script.Bindings)
            .FirstOrDefaultAsync(script => script.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Name = name.Trim();
        entity.StepsJson = stepsJson.Trim();
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        db.ScriptBindings.RemoveRange(entity.Bindings);
        entity.Bindings.Clear();

        foreach (ScriptUpsertBinding binding in bindings)
        {
            entity.Bindings.Add(CreateBindingEntity(entity.Id, binding));
        }

        await db.SaveChangesAsync(cancellationToken);
        return ToStored(entity);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        Script? entity = await db.Scripts
            .FirstOrDefaultAsync(script => script.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        db.Scripts.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<StoredScript>> ListMatchingAsync(
        ScriptEventKind eventKind,
        Guid? projectId,
        string? mode,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<Script> entities = await db.Scripts
            .AsNoTracking()
            .Include(script => script.Bindings)
            .Where(script =>
                script.ProjectId == null
                || (projectId != null && script.ProjectId == projectId))
            .ToListAsync(cancellationToken);

        string? normalizedMode = string.IsNullOrWhiteSpace(mode) ? null : mode.Trim();

        return entities
            .Select(script =>
            {
                List<ScriptBinding> matching = script.Bindings
                    .Where(binding =>
                        binding.Enabled
                        && binding.Event == eventKind
                        && ModeMatches(binding.ModeFilter, normalizedMode))
                    .OrderBy(binding => binding.Order)
                    .ToList();

                if (matching.Count == 0)
                {
                    return null;
                }

                return ToStored(script) with { Bindings = matching.Select(ToStoredBinding).ToArray() };
            })
            .Where(script => script is not null)
            .Cast<StoredScript>()
            .OrderBy(script => script.ProjectId.HasValue ? 0 : 1)
            .ThenBy(script => script.Bindings.Min(binding => binding.Order))
            .ThenBy(script => script.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool ModeMatches(string? modeFilter, string? mode)
    {
        if (string.IsNullOrWhiteSpace(modeFilter))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(mode))
        {
            return false;
        }

        return string.Equals(modeFilter, mode, StringComparison.OrdinalIgnoreCase);
    }

    private static ScriptBinding CreateBindingEntity(string scriptId, ScriptUpsertBinding binding) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            ScriptId = scriptId,
            Event = binding.Event,
            ModeFilter = string.IsNullOrWhiteSpace(binding.ModeFilter) ? null : binding.ModeFilter.Trim(),
            Order = binding.Order,
            Enabled = binding.Enabled,
            OnError = binding.OnError
        };

    private static StoredScript ToStored(Script entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.ProjectId,
            entity.StepsJson,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.Bindings
                .OrderBy(binding => binding.Order)
                .Select(ToStoredBinding)
                .ToArray());

    private static StoredScriptBinding ToStoredBinding(ScriptBinding binding) =>
        new(
            binding.Id,
            binding.ScriptId,
            binding.Event,
            binding.ModeFilter,
            binding.Order,
            binding.Enabled,
            binding.OnError);
}
