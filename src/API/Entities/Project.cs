namespace Orchi.Api.Entities;

public class Project
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public List<Workspace> Workspaces { get; set; } = [];
}
