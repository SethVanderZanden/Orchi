using Orchi.Api.Infrastructure.Agents.Cursor;
using Orchi.Api.Infrastructure.Agents.Persistence;

namespace Orchi.Api.Infrastructure.Agents;

public static class AgentsExtensions
{
    public static IServiceCollection AddOrchiAgents(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CursorAgentOptions>(configuration.GetSection(CursorAgentOptions.SectionName));

        services.AddSingleton<IChatStore, EfChatStore>();
        services.AddSingleton<AgentSessionManager>();
        services.AddSingleton<IAgentAdapter, CursorAgentAdapter>();
        services.AddSingleton<IAgentAdapterFactory, AgentAdapterFactory>();
        services.AddHostedService<AgentSessionShutdownService>();

        return services;
    }
}
