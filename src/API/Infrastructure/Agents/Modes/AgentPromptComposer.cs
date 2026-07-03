namespace Orchi.Api.Infrastructure.Agents.Modes;

public sealed class AgentPromptComposer(IAgentModeStrategyFactory modeStrategyFactory)
{
    public string Compose(string modeId, string userContent)
    {
        IAgentModeStrategy strategy = modeStrategyFactory.GetStrategy(modeId);
        return strategy.BuildPrompt(userContent);
    }

    public IReadOnlyList<string> GetExtraCliArgs(string modeId)
    {
        IAgentModeStrategy strategy = modeStrategyFactory.GetStrategy(modeId);
        return strategy.ExtraCliArgs;
    }
}
