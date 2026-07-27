using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Plans;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;
using Orchi.Api.Infrastructure.Agents.Plans.Persistence;

namespace Orchi.Api.Infrastructure.Agents.Orchestration;

public interface IOrchestrationPlanSyncService
{
    Task SyncFromMessagesAsync(ChatSession parent, CancellationToken cancellationToken);
}

public sealed class OrchestrationPlanSyncService(
    IPlanStore planStore,
    IOrchiArtifactWriterFactory artifactWriterFactory,
    OrchiArtifactFileStore artifactFileStore,
    ILogger<OrchestrationPlanSyncService> logger) : IOrchestrationPlanSyncService
{
    public async Task SyncFromMessagesAsync(ChatSession parent, CancellationToken cancellationToken)
    {
        if (!string.Equals(parent.Mode, OrchestrationAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase) ||
            parent.ParentChatId is not null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(parent.WorkspacePath))
        {
            logger.LogDebug(
                "Skipping plan sync for orchestration chat {ChatId}: workspace path is missing.",
                parent.Id);
            return;
        }

        IReadOnlyList<PlanMarkdownParser.ParsedPlan> plans =
            PlanMarkdownParser.ExtractAllPlansFromMessages(parent.Messages);

        if (plans.Count == 0)
        {
            return;
        }

        IOrchiArtifactWriterStrategy planWriter = artifactWriterFactory.GetStrategy(OrchiArtifactKind.Plan);

        foreach (PlanMarkdownParser.ParsedPlan plan in plans)
        {
            await planStore.UpsertAsync(
                new PlanUpsertModel(
                    plan.PlanId,
                    parent.Id,
                    plan.Title,
                    plan.ContentMarkdown),
                cancellationToken);

            await planWriter.WriteAsync(
                parent.WorkspacePath,
                plan.PlanId,
                plan.ContentMarkdown,
                cancellationToken);
        }

        IReadOnlyList<string> sequencePlanIds =
            PlanSequenceMarkdownParser.ParseSequenceFromMessages(parent.Messages);

        if (sequencePlanIds.Count > 0)
        {
            string sequenceContent = string.Join(
                Environment.NewLine,
                sequencePlanIds.Select(planId => planId.ToLowerInvariant()));

            await artifactFileStore.WriteAsync(
                parent.WorkspacePath,
                OrchiArtifactFileStore.PlanSequenceRelativePath,
                sequenceContent,
                cancellationToken);
        }

        logger.LogInformation(
            "Synced {PlanCount} plan file(s) for orchestration chat {ChatId}.",
            plans.Count,
            parent.Id);
    }
}
