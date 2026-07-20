namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// How to spawn an agent CLI after PATH/PATHEXT resolution.
/// Mirrors T3 Code's ResolvedSpawnCommand shape (command + optional entry script).
/// </summary>
internal sealed record AgentCliLaunchSpec(
    string ExecutablePath,
    string? EntryScript)
{
    public bool UsesNodeBundle => !string.IsNullOrWhiteSpace(EntryScript);

    public bool RequiresWindowsShell =>
        !UsesNodeBundle &&
        (Path.GetExtension(ExecutablePath).Equals(".cmd", StringComparison.OrdinalIgnoreCase) ||
         Path.GetExtension(ExecutablePath).Equals(".bat", StringComparison.OrdinalIgnoreCase));

    public string LaunchKind =>
        UsesNodeBundle ? "node-bundle" :
        Path.GetExtension(ExecutablePath).Equals(".cmd", StringComparison.OrdinalIgnoreCase) ? "cmd-shim" :
        Path.GetExtension(ExecutablePath).Equals(".bat", StringComparison.OrdinalIgnoreCase) ? "bat-shim" :
        "direct";
}
