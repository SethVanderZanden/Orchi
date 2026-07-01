using Orchi.SharedContext.Modes;

namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed record AgentTurnRequest(
    string StablePrefix,
    string DynamicContext,
    IReadOnlyList<string> ExtraCliArgs,
    CursorCliProfileKind CliProfileKind)
{
    public string PreparedPrompt => $"{StablePrefix}\n\n---\n\n{DynamicContext}";

    public static AgentTurnRequest FromUserContent(string userContent) =>
        new(userContent, userContent, [], CursorCliProfileKind.Agent);
}
