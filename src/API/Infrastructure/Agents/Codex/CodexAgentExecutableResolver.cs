using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal static class CodexAgentExecutableResolver
{
    private static readonly IAgentCliInstallLayout Layout = new CodexCliInstallLayout();

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

    public static ResolveResult Resolve(CodexAgentOptions options) =>
        Resolve(options, ExecutableEnvironment.Current);

    internal static ResolveResult Resolve(CodexAgentOptions options, IExecutableEnvironment environment) =>
        ResolveResult.From(
            AgentCliCommandResolver.Resolve(
                options.Executable,
                options.AdditionalSearchPaths,
                Layout,
                environment));

    internal static AgentCliLaunchSpec? TryResolveCodexBundle(
        string installDirectory,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths = null) =>
        Layout.TryResolveBundle(installDirectory, environment, searchedPaths);
}
