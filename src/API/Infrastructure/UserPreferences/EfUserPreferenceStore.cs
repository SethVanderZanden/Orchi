using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.UserPreferences;

public sealed class EfUserPreferenceStore(IDbContextFactory<AppDbContext> dbContextFactory)
    : IUserPreferenceStore
{
    public async Task<StoredUserPreference> GetOrCreateDefaultAsync(CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        UserPreference? entity = await db.UserPreferences
            .FirstOrDefaultAsync(preference => preference.Id == UserPreference.DefaultId, cancellationToken);

        if (entity is not null)
        {
            return ToStored(entity);
        }

        var created = new UserPreference
        {
            Id = UserPreference.DefaultId,
            PostMessageBehavior = PostMessageBehavior.StayOnChat,
            EnabledAgentIdsJson = EnabledAgentIdsSerializer.Serialize([]),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.UserPreferences.Add(created);
        await db.SaveChangesAsync(cancellationToken);

        return ToStored(created);
    }

    public async Task<StoredUserPreference> UpdateAsync(
        PostMessageBehavior? postMessageBehavior,
        IReadOnlyList<string>? enabledAgentIds,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        UserPreference? entity = await db.UserPreferences
            .FirstOrDefaultAsync(preference => preference.Id == UserPreference.DefaultId, cancellationToken);

        if (entity is null)
        {
            entity = CreateDefaultEntity();
            db.UserPreferences.Add(entity);
        }

        if (postMessageBehavior is not null)
        {
            entity.PostMessageBehavior = postMessageBehavior.Value;
        }

        if (enabledAgentIds is not null)
        {
            entity.EnabledAgentIdsJson = EnabledAgentIdsSerializer.Serialize(enabledAgentIds);
        }

        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return ToStored(entity);
    }

    private static UserPreference CreateDefaultEntity() =>
        new()
        {
            Id = UserPreference.DefaultId,
            PostMessageBehavior = PostMessageBehavior.StayOnChat,
            EnabledAgentIdsJson = EnabledAgentIdsSerializer.Serialize([]),
            UpdatedAt = DateTimeOffset.UtcNow
        };

    private static StoredUserPreference ToStored(UserPreference entity) =>
        new(
            entity.Id,
            entity.PostMessageBehavior,
            EnabledAgentIdsSerializer.Parse(entity.EnabledAgentIdsJson),
            entity.UpdatedAt);
}
