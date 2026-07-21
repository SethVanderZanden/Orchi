namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Strategy: maps one NDJSON stdout line from an agent CLI to normalized <see cref="AgentEvent"/> values.
/// </summary>
public interface IAgentStreamLineParser
{
    IEnumerable<AgentEvent> ParseLine(string line);
}
