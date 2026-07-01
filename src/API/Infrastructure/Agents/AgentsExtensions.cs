using Orchi.Api.Infrastructure.Agents.Cursor;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Coordination;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;
using Orchi.Api.Infrastructure.Agents.Modes.Strategies;
using Orchi.Api.Infrastructure.Agents.Persistence;

namespace Orchi.Api.Infrastructure.Agents;

public static class AgentsExtensions
{
    public static IServiceCollection AddOrchiAgents(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CursorAgentOptions>(configuration.GetSection(CursorAgentOptions.SectionName));

        services.AddSingleton<IChatStore, EfChatStore>();
        services.AddSingleton<IPlanStore, EfPlanStore>();
        services.AddSingleton<PlanManager>();
        services.AddSingleton<GoalCheckInQueue>();
        services.AddSingleton<AgentPromptComposer>();

        services.AddSingleton<IChatModeStrategy, AgentModeStrategy>();
        services.AddSingleton<IChatModeStrategy, PlanModeStrategy>();
        services.AddSingleton<IChatModeStrategy, ImplementModeStrategy>();
        services.AddSingleton<IChatModeStrategy, OrchestrateModeStrategy>();
        services.AddSingleton<IChatModeStrategy, GoalModeStrategy>();
        services.AddSingleton<IChatModeStrategy, ParticipantModeStrategy>();
        services.AddSingleton<ChatModeStrategyFactory>();

        services.AddSingleton<AgentSessionManager>();
        services.AddSingleton<IAgentAdapter, CursorAgentAdapter>();
        services.AddSingleton<IAgentAdapterFactory, AgentAdapterFactory>();
        services.AddHostedService<AgentSessionShutdownService>();
        services.AddHostedService<GoalCheckInWorker>();

        return services;
    }
}
