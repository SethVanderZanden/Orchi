using Orchi.Api.Infrastructure.SelectionActions;

namespace Orchi.Api.Features.SelectionActions.Shared;

public sealed record SelectionActionResponse(
    string Id,
    string Label,
    string Template,
    int SortOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public static class SelectionActionMapper
{
    public static SelectionActionResponse ToResponse(StoredSelectionAction action) =>
        new(action.Id, action.Label, action.Template, action.SortOrder, action.CreatedAt, action.UpdatedAt);
}
