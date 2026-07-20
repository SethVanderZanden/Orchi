using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal static class CursorAgentExecutableResolver
{
    private static readonly IAgentCliInstallLayout Layout = new CursorCliInstallLayout();

    // Alias for call sites / tests that still refer to the agent-specific result type.
    internal sealed record ResolveResult(
        bool Success,
        AgentCliLaunchSpec? Launch,
        string? ErrorMessage,
        AgentCliHostPlatform HostPlatform = AgentCliHostPlatform.Unknown,
        AgentCliInstallKind InstallKind = AgentCliInstallKind.Unknown,
        IReadOnlyList<string>? SearchedPaths = null)
    {
        public string LaunchKind => Launch?.LaunchKind ?? "none";

        public static ResolveResult From(AgentCliCommandResolver.ResolveResult result) =>
            new(
                result.Success,
                result.Launch,
                result.ErrorMessage,
                result.HostPlatform,
                result.InstallKind,
                result.SearchedPaths);
    }

    public static ResolveResult Resolve(CursorAgentOptions options) =>
        Resolve(options, ExecutableEnvironment.Current);

    internal static ResolveResult Resolve(CursorAgentOptions options, IExecutableEnvironment environment) =>
        ResolveResult.From(
            AgentCliCommandResolver.Resolve(
                options.Executable,
                options.AdditionalSearchPaths,
                Layout,
                environment));

    internal static AgentCliLaunchSpec? TryResolveNodeBundle(
        string installDirectory,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths = null) =>
        Layout.TryResolveBundle(installDirectory, environment, searchedPaths);
}
