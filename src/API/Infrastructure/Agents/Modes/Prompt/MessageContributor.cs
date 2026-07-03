namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public sealed class MessageContributor : IPromptSectionContributor
{
    public void Contribute(PromptBuildContext context, OrchiPromptDocument document) =>
        document.Message = context.UserContent;
}
