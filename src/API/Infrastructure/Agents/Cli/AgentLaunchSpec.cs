namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Normalized executable launch details shared by all CLI agent processors.
/// </summary>
public sealed record AgentLaunchSpec(
    string ExecutablePath,
    string? EntryScript)
{
    public bool UsesNodeBundle => !string.IsNullOrWhiteSpace(EntryScript);

    public bool UsesCmdShim =>
        !UsesNodeBundle &&
        (Path.GetExtension(ExecutablePath).Equals(".cmd", StringComparison.OrdinalIgnoreCase) ||
         Path.GetExtension(ExecutablePath).Equals(".bat", StringComparison.OrdinalIgnoreCase));

    public string LaunchKind =>
        UsesNodeBundle ? "node-bundle" :
        UsesCmdShim ? "cmd-shim" :
        "direct";
}
