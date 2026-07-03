using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed class ModeSectionContributor(IAgentModeStrategyFactory modeStrategyFactory) : IPromptSectionContributor
{
    public void Contribute(PromptBuildContext context, OrchiPromptDocument document)
    {
        IAgentModeStrategy strategy = modeStrategyFactory.GetStrategy(context.ModeId);
        strategy.ContributeSections(context, document);
    }
}
