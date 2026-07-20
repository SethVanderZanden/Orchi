using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal sealed record CachedCursorExecutableResolution(
    bool Success,
    string? ExecutablePath,
    string? EntryScript,
    string? ErrorMessage,
    AgentCliHostPlatform HostPlatform = AgentCliHostPlatform.Unknown,
    AgentCliInstallKind InstallKind = AgentCliInstallKind.Unknown)
{
    public static CachedCursorExecutableResolution From(CursorAgentExecutableResolver.ResolveResult result) =>
        new(
            result.Success,
            result.Launch?.ExecutablePath,
            result.Launch?.EntryScript,
            result.ErrorMessage,
            result.HostPlatform,
            result.InstallKind);

    public CursorAgentExecutableResolver.ResolveResult ToResolveResult()
    {
        if (Success && ExecutablePath is not null)
        {
            return new CursorAgentExecutableResolver.ResolveResult(
                true,
                new AgentCliLaunchSpec(ExecutablePath, EntryScript),
                null,
                HostPlatform,
                InstallKind);
        }

        return new CursorAgentExecutableResolver.ResolveResult(
            false,
            null,
            ErrorMessage,
            HostPlatform,
            InstallKind);
    }
}
