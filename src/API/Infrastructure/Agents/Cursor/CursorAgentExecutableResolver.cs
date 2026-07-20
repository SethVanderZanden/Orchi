using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal static class CursorAgentExecutableResolver
{
    private static readonly IAgentCliInstallLayout Layout = new CursorCliInstallLayout();

    internal sealed record ResolveResult(bool Success, AgentCliLaunchSpec? Launch, string? ErrorMessage);

    public static ResolveResult Resolve(CursorAgentOptions options) =>
        Resolve(options, ExecutableEnvironment.Current);

    internal static ResolveResult Resolve(CursorAgentOptions options, IExecutableEnvironment environment)
    {
        AgentCliCommandResolver.ResolveResult result = AgentCliCommandResolver.Resolve(
            options.Executable,
            options.AdditionalSearchPaths,
            Layout,
            environment);

        return new ResolveResult(result.Success, result.Launch, result.ErrorMessage);
    }

    internal static AgentCliLaunchSpec? TryResolveNodeBundle(
        string installDirectory,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths = null) =>
        Layout.TryResolveBundle(installDirectory, environment, searchedPaths);
}
