using Orchi.Api.Infrastructure.Agents.Attachments.Models;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed class AgentPromptComposer(
    IAgentModeStrategyFactory modeStrategyFactory,
    PromptSectionPipeline pipeline,
    OrchiPromptRenderer renderer) : IAgentPromptComposer
{
    public string Compose(
        ChatSession session,
        string userContent,
        AgentAttachmentContext? attachmentContext = null)
    {
        var context = new PromptBuildContext
        {
            ModeId = session.Mode,
            UserContent = userContent,
            WorkspacePath = session.WorkspacePath,
            PlanFilePath = session.PlanFilePath,
            ParentChatId = session.ParentChatId,
            IsFirstUserTurn = session.Messages.Count(message => message.Role == "user") <= 1,
            AttachmentContext = attachmentContext,
        };

        OrchiPromptDocument document = pipeline.Build(context);
        return renderer.Render(document);
    }

    public IReadOnlyList<string> GetExtraCliArgs(string modeId)
    {
        IAgentModeStrategy strategy = modeStrategyFactory.GetStrategy(modeId);
        return strategy.ExtraCliArgs;
    }
}
