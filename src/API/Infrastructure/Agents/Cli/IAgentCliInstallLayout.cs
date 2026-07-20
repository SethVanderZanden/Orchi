namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Agent-specific install layout (Strategy). Shared PATH / known-dir resolution lives in
/// <see cref="AgentCliCommandResolver"/>; each agent only describes where its CLI lives
/// and how to unwrap npm/native bundles. Host OS and install kind are auto-detected —
/// see <c>docs/patterns/agent-cli-platform-extensibility.md</c>.
/// </summary>
internal interface IAgentCliInstallLayout
{
    string AgentDisplayName { get; }

    string[] GetCandidateNames(string configuredExecutable);

    /// <summary>
    /// Agent-specific install directories for the current host platform (in addition to
    /// <see cref="AgentCliKnownDirectories"/>). Default: none.
    /// </summary>
    IEnumerable<string> GetPreferredInstallDirectories(IExecutableEnvironment environment) =>
        [];

    /// <summary>
    /// Last-resort absolute candidate files when PATH search fails (OS-specific).
    /// </summary>
    IEnumerable<string> GetFallbackPaths(
        IExecutableEnvironment environment,
        IReadOnlyList<string> candidateNames);

    AgentCliLaunchSpec? TryResolveBundle(
        string installDirectory,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths = null);
}
