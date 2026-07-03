using Orchi.Api.Infrastructure.Agents.Cursor;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.Api.Infrastructure.Agents.Plans;

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
        services.AddSingleton<IAgentModeStrategy, DefaultAgentModeStrategy>();
        services.AddSingleton<IAgentModeStrategy, OrchestrationAgentModeStrategy>();
        services.AddSingleton<IAgentModeStrategyFactory, AgentModeStrategyFactory>();
        services.AddSingleton<OrchiPromptRenderer>();
        services.AddSingleton<IPromptSectionContributor, ModeSectionContributor>();
        services.AddSingleton<IPromptSectionContributor, SessionContextContributor>();
        services.AddSingleton<IPromptSectionContributor, SessionTaskContributor>();
        services.AddSingleton<IPromptSectionContributor, GlobalRulesContributor>();
        services.AddSingleton<IPromptSectionContributor, MessageContributor>();
        services.AddSingleton<PromptSectionPipeline>();
        services.AddSingleton<AgentPromptComposer>();
        services.AddSingleton<IPlanFileWriter, PlanFileWriter>();
        services.AddHostedService<AgentSessionShutdownService>();

        return services;
    }
}
