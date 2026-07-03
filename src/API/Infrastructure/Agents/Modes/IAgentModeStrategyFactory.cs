namespace Orchi.Api.Infrastructure.Agents.Modes;

public interface IAgentModeStrategyFactory
{
    IAgentModeStrategy GetStrategy(string modeId);
}
