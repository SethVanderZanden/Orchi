namespace Orchi.Api.Entities;

public class PipelineRun
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
}
