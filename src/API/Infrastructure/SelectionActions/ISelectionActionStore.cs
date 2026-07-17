namespace Orchi.Api.Infrastructure.SelectionActions;

public sealed record StoredSelectionAction(
    string Id,
    string Label,
    string Template,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public interface ISelectionActionStore
{
    Task<IReadOnlyList<StoredSelectionAction>> ListAsync(CancellationToken cancellationToken);

    Task<StoredSelectionAction?> GetAsync(string id, CancellationToken cancellationToken);

    Task<StoredSelectionAction> CreateAsync(
        string label,
        string template,
        CancellationToken cancellationToken);

    Task<StoredSelectionAction?> UpdateAsync(
        string id,
        string label,
        string template,
        int? sortOrder,
        CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string id, CancellationToken cancellationToken);
}
