using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.SelectionActions;

public sealed class EfSelectionActionStore(IDbContextFactory<AppDbContext> dbContextFactory)
    : ISelectionActionStore
{
    public async Task<IReadOnlyList<StoredSelectionAction>> ListAsync(CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // SQLite cannot ORDER BY DateTimeOffset in SQL — order in memory (same as EfChatStore).
        List<SelectionAction> entities = await db.SelectionActions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entities
            .OrderBy(action => action.SortOrder)
            .ThenBy(action => action.CreatedAt)
            .Select(ToStored)
            .ToArray();
    }

    public async Task<StoredSelectionAction?> GetAsync(string id, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        SelectionAction? entity = await db.SelectionActions
            .AsNoTracking()
            .FirstOrDefaultAsync(action => action.Id == id, cancellationToken);

        return entity is null ? null : ToStored(entity);
    }

    public async Task<StoredSelectionAction> CreateAsync(
        string label,
        string template,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        int nextSortOrder = await db.SelectionActions.AnyAsync(cancellationToken)
            ? await db.SelectionActions.MaxAsync(action => action.SortOrder, cancellationToken) + 1
            : 0;

        DateTimeOffset now = DateTimeOffset.UtcNow;
        var entity = new SelectionAction
        {
            Id = Guid.NewGuid().ToString("N"),
            Label = label.Trim(),
            Template = template.Trim(),
            SortOrder = nextSortOrder,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.SelectionActions.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToStored(entity);
    }

    public async Task<StoredSelectionAction?> UpdateAsync(
        string id,
        string label,
        string template,
        int? sortOrder,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        SelectionAction? entity = await db.SelectionActions
            .FirstOrDefaultAsync(action => action.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        entity.Label = label.Trim();
        entity.Template = template.Trim();
        if (sortOrder is not null)
        {
            entity.SortOrder = sortOrder.Value;
        }

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return ToStored(entity);
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        SelectionAction? entity = await db.SelectionActions
            .FirstOrDefaultAsync(action => action.Id == id, cancellationToken);

        if (entity is null)
        {
            return false;
        }

        db.SelectionActions.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static StoredSelectionAction ToStored(SelectionAction entity) =>
        new(
            entity.Id,
            entity.Label,
            entity.Template,
            entity.SortOrder,
            entity.CreatedAt,
            entity.UpdatedAt);
}
