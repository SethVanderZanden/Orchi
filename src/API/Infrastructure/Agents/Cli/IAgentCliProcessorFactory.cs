namespace Orchi.Api.Infrastructure.Agents.Cli;

public interface IAgentCliProcessorFactory
{
    IAgentCliProcessorProfile GetProfile(string agentId);
}
