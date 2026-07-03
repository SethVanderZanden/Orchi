namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public interface IPromptSectionContributor
{
    void Contribute(PromptBuildContext context, OrchiPromptDocument document);
}
