using Orchi.Api.Infrastructure.Caching;
using Orchi.Api.Infrastructure.Agents.Cursor;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Models;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt.Behaviours;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;
using Orchi.Api.Infrastructure.Agents.Plans.Persistence;
using Orchi.Api.Infrastructure.Agents.Orchestration;
using Orchi.Api.Infrastructure.Agents.Orchestration.Handlers;
using Orchi.Api.Infrastructure.Agents.Orchestration.Persistence;
using Orchi.Api.Infrastructure.Agents.Workspace;
using Orchi.Api.Infrastructure.Projects;
using Scrutor;

namespace Orchi.Api.Infrastructure.Agents;

public static class AgentsExtensions
{
    public static IServiceCollection AddOrchiAgents(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CursorAgentOptions>(configuration.GetSection(CursorAgentOptions.SectionName));
        services.Configure<AgentModelCatalogOptions>(configuration.GetSection(AgentModelCatalogOptions.SectionName));

        services.AddSingleton<IChatStore, EfChatStore>();
        services.AddSingleton<IAgentModelStore, EfAgentModelStore>();
        services.AddSingleton<IAgentModeModelDefaultStore, EfAgentModeModelDefaultStore>();
        services.AddSingleton<IAgentModelListProvider, CursorAgentModelListProvider>();
        services.AddSingleton<AgentModelListProviderFactory>();
        services.AddSingleton<IAgentModelCatalogService, AgentModelCatalogService>();
        services.AddSingleton<IAgentModeModelDefaultService, AgentModeModelDefaultService>();
        services.AddSingleton<IProjectStore, EfProjectStore>();
        services.AddSingleton<EfPlanStore>();
        services.AddSingleton<IPlanStore, CachingPlanStore>();
        services.AddSingleton<EfOrchestrationWorkflowStore>();
        services.AddSingleton<IOrchestrationWorkflowStore, EfOrchestrationWorkflowStore>();
        services.AddSingleton<OrchestrationEventHub>();
        services.AddSingleton<IOrchestrationStepHandler, ReviewKickoffStepHandler>();
        services.AddSingleton<IOrchestrationStepHandler, SequentialAdvanceStepHandler>();
        services.AddSingleton<OrchestrationStepPipeline>();
        services.AddScoped<OrchestrationAgentRunner>();
        services.AddSingleton<IOrchiKickoffExecutor, OrchiKickoffExecutor>();
        services.AddSingleton<IOrchestrationWorkflowService, OrchestrationWorkflowService>();
        services.AddSingleton<IAgentTurnCompletionNotifier, AgentTurnCompletionNotifier>();
        services.AddSingleton<AgentSessionManager>();
        services.AddSingleton<IAgentAdapter, CursorAgentAdapter>();
        services.AddSingleton<IAgentAdapterFactory, AgentAdapterFactory>();
        services.AddSingleton<IAgentModeStrategy, DefaultAgentModeStrategy>();
        services.AddSingleton<IAgentModeStrategy, OrchestrationAgentModeStrategy>();
        services.AddSingleton<IAgentModeStrategy, ReviewAgentModeStrategy>();
        services.AddSingleton<IAgentModeStrategy, ImplementationAgentModeStrategy>();
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
        services.AddSingleton<GitWorkspaceDiffProvider>();
        services.AddSingleton<IWorkspaceDiffProvider>(sp =>
            new CachingWorkspaceDiffProvider(
                sp.GetRequiredService<GitWorkspaceDiffProvider>(),
                sp.GetRequiredService<OrchiHybridCacheService>()));
        services.AddHostedService<AgentSessionShutdownService>();

        return services;
    }
}
