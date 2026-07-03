using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

using Orchi.Api.Infrastructure.Agents.Workspace;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

internal sealed class FakeWorkspaceDiffProvider : IWorkspaceDiffProvider
{
    public string Diff { get; init; } = "diff --git a/file.txt b/file.txt";

    public string GetDiff(string workspacePath) => Diff;
}

internal static class PromptTestHelpers
{
    internal static AgentPromptComposer CreateComposer()
    {
        IAgentModeStrategyFactory factory = new AgentModeStrategyFactory([
            new DefaultAgentModeStrategy(),
            new OrchestrationAgentModeStrategy(),
            new ReviewAgentModeStrategy()
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
            new OrchestrationAgentModeStrategy(),
            new ReviewAgentModeStrategy()
        ]);

        return new PromptSectionPipeline([
            new ModeSectionContributor(factory),
            new SessionContextContributor(),
            new ReviewDiffContributor(new FakeWorkspaceDiffProvider()),
            new SessionTaskContributor(CreateArtifactTaskFactory()),
            new ParentChatContributor(),
            new GlobalRulesContributor(),
            new MessageContributor(),
        ]);
    }

    private static IOrchiArtifactTaskFactory CreateArtifactTaskFactory()
    {
        var fileStore = new OrchiArtifactFileStore();
        var writerFactory = new OrchiArtifactWriterFactory([
            new ImplementationPlanWriterStrategy(fileStore),
            new ReviewBriefWriterStrategy(fileStore)
        ]);

        return new OrchiArtifactTaskFactory([
            new ImplementationPlanTaskStrategy(),
            new ReviewPlanTaskStrategy()
        ], writerFactory);
    }

    internal static ChatSession CreateSession(
        string mode = DefaultAgentModeStrategy.Mode,
        string workspacePath = "/workspace",
        string? planFilePath = null,
        Guid? parentChatId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            AgentId = "cursor",
            WorkspacePath = workspacePath,
            Mode = mode,
            PlanFilePath = planFilePath,
            ParentChatId = parentChatId,
        };
}
