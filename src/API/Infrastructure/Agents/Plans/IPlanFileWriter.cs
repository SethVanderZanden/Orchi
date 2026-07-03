namespace Orchi.Api.Infrastructure.Agents.Plans;

public interface IPlanFileWriter
{
    Task<string> WritePlanAsync(
        string workspacePath,
        string planId,
        string contentMarkdown,
        CancellationToken cancellationToken = default);
}
