using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal static class CodexAgentExecutableResolver
{
    private static readonly IAgentCliInstallLayout Layout = new CodexCliInstallLayout();

    internal sealed record ResolveResult(bool Success, AgentCliLaunchSpec? Launch, string? ErrorMessage);

    public static ResolveResult Resolve(CodexAgentOptions options) =>
        Resolve(options, ExecutableEnvironment.Current);

    internal static ResolveResult Resolve(CodexAgentOptions options, IExecutableEnvironment environment)
    {
        AgentCliCommandResolver.ResolveResult result = AgentCliCommandResolver.Resolve(
            options.Executable,
            options.AdditionalSearchPaths,
            Layout,
            environment);

        return new ResolveResult(result.Success, result.Launch, result.ErrorMessage);
    }

    internal static AgentCliLaunchSpec? TryResolveCodexBundle(
        string installDirectory,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths = null) =>
        Layout.TryResolveBundle(installDirectory, environment, searchedPaths);
}
