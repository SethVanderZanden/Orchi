using Orchi.Api.Infrastructure.Caching;
using Orchi.Api.Infrastructure.Agents.Codex;
using Orchi.Api.Infrastructure.Agents.Cursor;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Models;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt.Behaviours;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.Api.Infrastructure.Agents.Search;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;
using Orchi.Api.Infrastructure.Agents.Plans.Persistence;
using Orchi.Api.Infrastructure.Agents.Orchestration;
using Orchi.Api.Infrastructure.Agents.Orchestration.Handlers;
using Orchi.Api.Infrastructure.Agents.Orchestration.Persistence;
using Orchi.Api.Infrastructure.Agents.Workspace;
using Orchi.Api.Infrastructure.Git.Hosting;
using Orchi.Api.Infrastructure.Git.Workspace;
using Orchi.Api.Infrastructure.Cli;
using Orchi.Api.Infrastructure.Projects;
using Orchi.Api.Infrastructure.Scripts;
using Orchi.Api.Infrastructure.Scripts.Actions;
using Orchi.Api.Infrastructure.SelectionActions;
using Scrutor;

namespace Orchi.Api.Infrastructure.Agents;

public static class AgentsExtensions
{
    public static IServiceCollection AddOrchiAgents(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CursorAgentOptions>(configuration.GetSection(CursorAgentOptions.SectionName));
        services.Configure<CodexAgentOptions>(configuration.GetSection(CodexAgentOptions.SectionName));
        services.Configure<AgentModelCatalogOptions>(configuration.GetSection(AgentModelCatalogOptions.SectionName));

        services.AddSingleton<IChatSearchClause, TextMatchChatSearchClause>();
        services.AddSingleton<ChatSearchComposer>();
        services.AddSingleton<IChatStore, EfChatStore>();
        services.AddSingleton<IAgentModelStore, EfAgentModelStore>();
        services.AddSingleton<IAgentContextSizeStore, EfAgentContextSizeStore>();
        services.AddSingleton<IAgentCliOptionStore, EfAgentCliOptionStore>();
        services.AddSingleton<IModeRuntimeDefaultStore, EfModeRuntimeDefaultStore>();
        services.AddSingleton<IAgentModelListProvider, CursorAgentModelListProvider>();
        services.AddSingleton<IAgentModelListProvider, CodexAgentModelListProvider>();
        services.AddSingleton<AgentModelListProviderFactory>();
        services.AddSingleton<IAgentModelCatalogService, AgentModelCatalogService>();
        services.AddSingleton<IAgentContextSizeCatalogService, AgentContextSizeCatalogService>();
        services.AddSingleton<IAgentCliOptionCatalogService, AgentCliOptionCatalogService>();
        services.AddSingleton<IModeRuntimeDefaultService, ModeRuntimeDefaultService>();
        services.AddSingleton<IProjectStore, EfProjectStore>();
        services.AddSingleton<ISelectionActionStore, EfSelectionActionStore>();
        services.AddSingleton<IScriptStore, EfScriptStore>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();
        services.AddSingleton<IGitWorkspaceService, GitWorkspaceService>();
        services.AddSingleton<IGitCommitMessageGenerator, GitCommitMessageGenerator>();
        services.AddSingleton<IGitHostAdapter, GitHubHostAdapter>();
        services.AddSingleton<IGitHostAdapter, AzureDevOpsHostAdapter>();
        services.AddSingleton<IGitHostAdapterFactory, GitHostAdapterFactory>();
        services.AddSingleton<IGitHostingFacade, GitHostingFacade>();
        services.AddSingleton<IScriptActionStrategy, ShellScriptActionStrategy>();
        services.AddSingleton<IScriptActionStrategy, GitCommitScriptActionStrategy>();
        services.AddSingleton<IScriptActionStrategy, GitPushScriptActionStrategy>();
        services.AddSingleton<IScriptActionStrategy, GitMergeScriptActionStrategy>();
        services.AddSingleton<IScriptActionStrategy, GitCreatePullRequestScriptActionStrategy>();
        services.AddSingleton<IScriptActionStrategy, GitWorktreeScriptActionStrategy>();
        services.AddSingleton<IScriptActionStrategyFactory, ScriptActionStrategyFactory>();
        services.AddSingleton<IScriptEventDispatcher, ScriptEventDispatcher>();
        services.AddSingleton<EfPlanStore>();
        services.AddSingleton<IPlanStore, CachingPlanStore>();
        services.AddSingleton<EfOrchestrationWorkflowStore>();
        services.AddSingleton<IOrchestrationWorkflowStore, EfOrchestrationWorkflowStore>();
        services.AddSingleton<OrchestrationEventHub>();
        services.AddSingleton<ChatStatusEventHub>();
        services.AddSingleton<IChatStatusService, ChatStatusService>();
        services.AddSingleton<IOrchestrationStepHandler, ReviewKickoffStepHandler>();
        services.AddSingleton<IOrchestrationStepHandler, SequentialAdvanceStepHandler>();
        services.AddSingleton<OrchestrationStepPipeline>();
        services.AddScoped<OrchestrationAgentRunner>();
        services.AddSingleton<IOrchiKickoffExecutor, OrchiKickoffExecutor>();
        services.AddSingleton<IOrchestrationWorkflowService, OrchestrationWorkflowService>();
        services.AddSingleton<IAgentTurnCompletionNotifier, AgentTurnCompletionNotifier>();
        services.AddSingleton<AgentSessionManager>();
        services.AddSingleton<IAgentAdapter, CursorAgentAdapter>();
        services.AddSingleton<IAgentAdapter, CodexAgentAdapter>();
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
