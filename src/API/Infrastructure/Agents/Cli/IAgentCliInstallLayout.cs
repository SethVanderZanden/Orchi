namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Agent-specific install layout (Strategy). Shared PATH/PATHEXT resolution lives in
/// <see cref="AgentCliCommandResolver"/>; each agent only describes where its CLI lives
/// and how to unwrap npm/native bundles — same split T3 Code uses between shell.ts and
/// per-provider Drivers (e.g. ClaudeExecutable following .cmd → claude.exe).
/// </summary>
internal interface IAgentCliInstallLayout
{
    string AgentDisplayName { get; }

    string[] GetCandidateNames(string configuredExecutable);

    IEnumerable<string> GetPreferredInstallDirectories(IExecutableEnvironment environment);

    IEnumerable<string> GetWindowsFallbackPaths(
        IExecutableEnvironment environment,
        IReadOnlyList<string> candidateNames);

    AgentCliLaunchSpec? TryResolveBundle(
        string installDirectory,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths = null);
}
