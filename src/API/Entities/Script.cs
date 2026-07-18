namespace Orchi.Api.Entities;

public class Script
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Null for global scripts; set for project-scoped scripts.</summary>
    public Guid? ProjectId { get; set; }

    /// <summary>JSON array of typed script steps.</summary>
    public required string StepsJson { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public Project? Project { get; set; }

    public List<ScriptBinding> Bindings { get; set; } = [];
}
