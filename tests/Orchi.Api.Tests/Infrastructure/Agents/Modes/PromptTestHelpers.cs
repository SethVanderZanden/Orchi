using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

internal static class PromptTestHelpers
{
    internal static AgentPromptComposer CreateComposer()
    {
        IAgentModeStrategyFactory factory = new AgentModeStrategyFactory([
            new DefaultAgentModeStrategy(),
            new OrchestrationAgentModeStrategy()
        ]);

        return new AgentPromptComposer(
            factory,
            CreatePipeline(factory),
            new OrchiPromptRenderer());
    }

    internal static PromptSectionPipeline CreatePipeline(IAgentModeStrategyFactory? factory = null)
    {
        factory ??= new AgentModeStrategyFactory([
            new DefaultAgentModeStrategy(),
            new OrchestrationAgentModeStrategy()
        ]);

        return new PromptSectionPipeline([
            new ModeSectionContributor(factory),
            new SessionContextContributor(),
            new SessionTaskContributor(),
            new GlobalRulesContributor(),
            new MessageContributor(),
        ]);
    }

    internal static ChatSession CreateSession(
        string mode = DefaultAgentModeStrategy.Mode,
        string workspacePath = "/workspace",
        string? planFilePath = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            AgentId = "cursor",
            WorkspacePath = workspacePath,
            Mode = mode,
            PlanFilePath = planFilePath,
        };
}
