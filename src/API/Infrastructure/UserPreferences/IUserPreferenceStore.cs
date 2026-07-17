using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.UserPreferences;

public sealed record StoredUserPreference(
    string Id,
    PostMessageBehavior PostMessageBehavior,
    IReadOnlyList<string> EnabledAgentIds,
    DateTimeOffset UpdatedAt);

public interface IUserPreferenceStore
{
    Task<StoredUserPreference> GetOrCreateDefaultAsync(CancellationToken cancellationToken);

    Task<StoredUserPreference> UpdateAsync(
        PostMessageBehavior? postMessageBehavior,
        IReadOnlyList<string>? enabledAgentIds,
        CancellationToken cancellationToken);
}
