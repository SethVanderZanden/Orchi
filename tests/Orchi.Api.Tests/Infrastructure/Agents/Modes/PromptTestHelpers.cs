using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Prompt;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

using Orchi.Api.Infrastructure.Agents.Workspace;

namespace Orchi.Api.Tests.Infrastructure.Agents.Modes;

internal sealed class FakeWorkspaceDiffProvider : IWorkspaceDiffProvider
{
    public string Diff { get; init; } = "diff --git a/file.txt b/file.txt";

    public string BranchDiff { get; init; } = "diff --git a/branch.txt b/branch.txt";

    public string GetDiff(string workspacePath) => Diff;

    public string GetBranchDiff(string workspacePath, string baseBranch, string headBranch) =>
        $"{BranchDiff}\n# {baseBranch}...{headBranch}";
}

internal static class PromptTestHelpers
{
    internal static AgentPromptComposer CreateComposer()
    {
        IAgentModeStrategyFactory factory = new AgentModeStrategyFactory([
            new DefaultAgentModeStrategy(),
            new OrchestrationAgentModeStrategy(),
            new ReviewAgentModeStrategy(),
            new ImplementationAgentModeStrategy(),
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
            new ReviewAgentModeStrategy(),
            new ImplementationAgentModeStrategy(),
        ]);

        return new PromptSectionPipeline([
            new ModeSectionContributor(factory),
            new SessionContextContributor(),
            new ReviewDiffContributor(CreateReviewDiffAdapterResolver()),
            new SessionTaskContributor(CreateArtifactTaskFactory()),
            new ParentChatContributor(),
            new GlobalRulesContributor(),
            new MessageContributor(),
        ]);
    }

    private static IReviewDiffAdapterResolver CreateReviewDiffAdapterResolver(
        IWorkspaceDiffProvider? diffProvider = null)
    {
        IWorkspaceDiffProvider provider = diffProvider ?? new FakeWorkspaceDiffProvider();
        return new ReviewDiffAdapterResolver([
            new BranchPairReviewDiffAdapter(provider),
            new WorkspaceHeadReviewDiffAdapter(provider),
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
        Guid? parentChatId = null,
        IReadOnlyList<ChatMessage>? messages = null)
    {
        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            AgentId = "cursor",
            WorkspacePath = workspacePath,
            Mode = mode,
            PlanFilePath = planFilePath,
            ParentChatId = parentChatId,
        };

        if (messages is not null)
        {
            session.Messages.AddRange(messages);
        }

        return session;
    }
}
