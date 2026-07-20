namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// npm-style platform package coordinates used when unwrapping native CLI binaries
/// next to a global install (auto-selected from host OS + architecture).
/// </summary>
internal sealed record AgentCliPlatformPackage(
    AgentCliHostPlatform Platform,
    AgentCliHostArchitecture Architecture,
    string PackageName,
    string RelativeExecutablePath);

internal static class AgentCliPlatformPackages
{
    public static IEnumerable<AgentCliPlatformPackage> ForHost(
        IEnumerable<AgentCliPlatformPackage> catalog,
        IExecutableEnvironment environment) =>
        catalog.Where(package =>
            package.Platform == environment.HostPlatform &&
            (package.Architecture == environment.HostArchitecture ||
             package.Architecture == AgentCliHostArchitecture.Unknown));
}
