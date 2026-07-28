using Orchi.Api.Infrastructure.Agents.Attachments.Models;

namespace Orchi.Api.Infrastructure.Agents.Modes;

public interface IAgentPromptComposer
{
    string Compose(
        ChatSession session,
        string userContent,
        AgentAttachmentContext? attachmentContext = null);

    IReadOnlyList<string> GetExtraCliArgs(string modeId);
}
