namespace Orchi.Api.Infrastructure.Agents;

public interface IAgentAdapterFactory
{
    IAgentAdapter GetAdapter(string agentId);
}
