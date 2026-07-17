namespace Orchi.Api.Entities;

public class SelectionAction
{
    public required string Id { get; set; }

    public required string Label { get; set; }

    /// <summary>
    /// Prompt template. Must include the <c>{{selected text}}</c> placeholder.
    /// </summary>
    public required string Template { get; set; }

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
