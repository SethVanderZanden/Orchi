namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public sealed class ParentChatContributor : IPromptSectionContributor
{
    public void Contribute(PromptBuildContext context, OrchiPromptDocument document)
    {
        if (context.ParentChatId is null)
        {
            return;
        }

        document.AppendContext($"Parent chat: {context.ParentChatId}");
    }
}
