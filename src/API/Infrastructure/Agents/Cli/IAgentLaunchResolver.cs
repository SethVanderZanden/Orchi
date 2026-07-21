namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Strategy: locates the agent CLI executable (and optional npm node-bundle entry script).
/// </summary>
public interface IAgentLaunchResolver
{
    string AgentId { get; }

    ValueTask<AgentLaunchResolveResult> ResolveAsync(CancellationToken cancellationToken);
}
