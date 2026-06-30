namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed record AgentTurnRequest(string PreparedPrompt, IReadOnlyList<string> ExtraCliArgs)
{
    public static AgentTurnRequest FromUserContent(string userContent) =>
        new(userContent, []);
}
