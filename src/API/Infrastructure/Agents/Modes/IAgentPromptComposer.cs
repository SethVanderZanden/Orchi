namespace Orchi.Api.Infrastructure.Agents.Modes;

public interface IAgentPromptComposer
{
    string Compose(ChatSession session, string userContent);

    IReadOnlyList<string> GetExtraCliArgs(string modeId);
}
