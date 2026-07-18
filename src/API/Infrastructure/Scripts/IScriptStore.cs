using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Scripts;

public sealed record StoredScriptBinding(
    string Id,
    string ScriptId,
    ScriptEventKind Event,
    string? ModeFilter,
    int Order,
    bool Enabled,
    ScriptOnError OnError);

public sealed record StoredScript(
    string Id,
    string Name,
    Guid? ProjectId,
    string StepsJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<StoredScriptBinding> Bindings);

public sealed record ScriptUpsertBinding(
    ScriptEventKind Event,
    string? ModeFilter,
    int Order,
    bool Enabled,
    ScriptOnError OnError);

public interface IScriptStore
{
    Task<IReadOnlyList<StoredScript>> ListAsync(Guid? projectId, CancellationToken cancellationToken);

    Task<StoredScript?> GetAsync(string id, CancellationToken cancellationToken);

    Task<StoredScript> CreateAsync(
        string name,
        Guid? projectId,
        string stepsJson,
        IReadOnlyList<ScriptUpsertBinding> bindings,
        CancellationToken cancellationToken);

    Task<StoredScript?> UpdateAsync(
        string id,
        string name,
        string stepsJson,
        IReadOnlyList<ScriptUpsertBinding> bindings,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);

    Task<IReadOnlyList<StoredScript>> ListMatchingAsync(
        ScriptEventKind eventKind,
        Guid? projectId,
        string? mode,
        CancellationToken cancellationToken);
}
