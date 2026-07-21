namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Bundles per-agent strategies used by <see cref="AgentCliTurnProcessor"/>.
/// Register one profile per agent; the factory resolves by <see cref="AgentId"/>.
/// </summary>
public interface IAgentCliProcessorProfile
{
    string AgentId { get; }

    /// <summary>Human-readable name for log and error messages (e.g. "Codex", "Cursor").</summary>
    string DisplayName { get; }

    int TimeoutSeconds { get; }

    /// <summary>
    /// When true, stderr is surfaced as <c>Agent.NoEvents</c> if stdout produced no parsed events.
    /// </summary>
    bool SurfaceStderrWhenNoParsedEvents { get; }

    /// <summary>
    /// When true, Windows <c>.cmd</c>/<c>.bat</c> shims run via <c>cmd.exe /c</c>.
    /// </summary>
    bool UseWindowsCmdShim { get; }

    IAgentLaunchResolver LaunchResolver { get; }

    IAgentCliArgumentBuilder ArgumentBuilder { get; }

    /// <summary>
    /// Factory for a fresh parser instance (stateful parsers e.g. Codex incremental text).
    /// </summary>
    IAgentStreamLineParser CreateLineParser();
}
