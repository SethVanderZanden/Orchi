namespace Orchi.Api.Infrastructure.Agents;

public static class AgentIds
{
    public const string Cursor = "cursor";

    public const string Codex = "codex";

    public static string DisplayLabel(string agentId) =>
        agentId.Trim().ToLowerInvariant() switch
        {
            Cursor => "Cursor",
            Codex => "Codex",
            _ => agentId
        };
}
