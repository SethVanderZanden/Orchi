namespace Orchi.Api.Infrastructure.Agents.Modes.Prompt;

public sealed class GlobalRulesContributor : IPromptSectionContributor
{
    public void Contribute(PromptBuildContext context, OrchiPromptDocument document) =>
        document.AppendRules(GlobalPromptRules.MetaRule);
}
