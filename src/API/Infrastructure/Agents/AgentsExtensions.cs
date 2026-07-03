using Orchi.Api.Infrastructure.Agents.Cursor;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt.Behaviours;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;
using Orchi.Api.Infrastructure.Agents.Plans.Persistence;
using Orchi.Api.Infrastructure.Agents.Workspace;
using Scrutor;

namespace Orchi.Api.Infrastructure.Agents;

public static class AgentsExtensions
{
    public static IServiceCollection AddOrchiAgents(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CursorAgentOptions>(configuration.GetSection(CursorAgentOptions.SectionName));

        services.AddSingleton<IChatStore, EfChatStore>();
        services.AddSingleton<IPlanStore, EfPlanStore>();
        services.AddSingleton<AgentSessionManager>();
        services.AddSingleton<IAgentAdapter, CursorAgentAdapter>();
        services.AddSingleton<IAgentAdapterFactory, AgentAdapterFactory>();
        services.AddSingleton<IAgentModeStrategy, DefaultAgentModeStrategy>();
        services.AddSingleton<IAgentModeStrategy, OrchestrationAgentModeStrategy>();
        services.AddSingleton<IAgentModeStrategy, ReviewAgentModeStrategy>();
        services.AddSingleton<IAgentModeStrategyFactory, AgentModeStrategyFactory>();
        services.AddSingleton<OrchiPromptRenderer>();
        services.AddSingleton<IPromptSectionContributor, ModeSectionContributor>();
        services.AddSingleton<IPromptSectionContributor, SessionContextContributor>();
        services.AddSingleton<IPromptSectionContributor, ReviewDiffContributor>();
        services.AddSingleton<IPromptSectionContributor, SessionTaskContributor>();
        services.AddSingleton<IPromptSectionContributor, ParentChatContributor>();
        services.AddSingleton<IPromptSectionContributor, GlobalRulesContributor>();
        services.AddSingleton<IPromptSectionContributor, MessageContributor>();
        services.AddSingleton<PromptSectionPipeline>();
        services.AddSingleton<IAgentPromptComposer, AgentPromptComposer>();
        services.Decorate<IAgentPromptComposer, LoggingPromptComposer>();
        services.AddSingleton<OrchiArtifactFileStore>();
        services.AddSingleton<IOrchiArtifactWriterStrategy, ImplementationPlanWriterStrategy>();
        services.AddSingleton<IOrchiArtifactWriterStrategy, ReviewBriefWriterStrategy>();
        services.AddSingleton<IOrchiArtifactWriterFactory, OrchiArtifactWriterFactory>();
        services.AddSingleton<IOrchiArtifactTaskStrategy, ImplementationPlanTaskStrategy>();
        services.AddSingleton<IOrchiArtifactTaskStrategy, ReviewPlanTaskStrategy>();
        services.AddSingleton<IOrchiArtifactTaskFactory, OrchiArtifactTaskFactory>();
        services.AddSingleton<IWorkspaceDiffProvider, GitWorkspaceDiffProvider>();
        services.AddHostedService<AgentSessionShutdownService>();

        return services;
    }
}
