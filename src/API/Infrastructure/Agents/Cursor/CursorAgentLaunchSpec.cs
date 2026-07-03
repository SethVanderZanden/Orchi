namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal sealed record CursorAgentLaunchSpec(
    string ExecutablePath,
    string? EntryScript)
{
    public bool UsesNodeBundle => !string.IsNullOrWhiteSpace(EntryScript);

    public string LaunchKind =>
        UsesNodeBundle ? "node-bundle" :
        Path.GetExtension(ExecutablePath).Equals(".cmd", StringComparison.OrdinalIgnoreCase) ? "cmd-shim" :
        Path.GetExtension(ExecutablePath).Equals(".bat", StringComparison.OrdinalIgnoreCase) ? "bat-shim" :
        "direct";
}
