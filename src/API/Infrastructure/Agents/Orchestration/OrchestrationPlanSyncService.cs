using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Plans;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;
using Orchi.Api.Infrastructure.Agents.Plans.Persistence;

namespace Orchi.Api.Infrastructure.Agents.Orchestration;

public interface IOrchestrationPlanSyncService
{
    Task SyncFromWorkspaceAsync(ChatSession parent, CancellationToken cancellationToken);
}

public sealed class OrchestrationPlanSyncService(
    IPlanStore planStore,
    IOrchiArtifactWriterFactory artifactWriterFactory,
    OrchiArtifactFileStore artifactFileStore,
    ILogger<OrchestrationPlanSyncService> logger) : IOrchestrationPlanSyncService
{
    public async Task SyncFromWorkspaceAsync(ChatSession parent, CancellationToken cancellationToken)
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
            await PlanMarkdownParser.ResolvePlansFromWorkspaceAndMessagesAsync(
                parent.WorkspacePath,
                parent.Messages,
                artifactFileStore,
                cancellationToken);

        if (plans.Count == 0)
        {
            return;
        }

        IOrchiArtifactWriterStrategy planWriter = artifactWriterFactory.GetStrategy(OrchiArtifactKind.Plan);
        int syncedCount = 0;

        foreach (PlanMarkdownParser.ParsedPlan plan in plans)
        {
            if (string.IsNullOrWhiteSpace(plan.ContentMarkdown))
            {
                logger.LogDebug(
                    "Skipping plan {PlanId} for chat {ChatId}: plan file is missing or empty.",
                    plan.PlanId,
                    parent.Id);
                continue;
            }

            await planStore.UpsertAsync(
                new PlanUpsertModel(
                    plan.PlanId,
                    parent.Id,
                    plan.Title,
                    plan.ContentMarkdown),
                cancellationToken);

            if (plan.PlanFilePath is null)
            {
                await planWriter.WriteAsync(
                    parent.WorkspacePath,
                    plan.PlanId,
                    plan.ContentMarkdown,
                    cancellationToken);
            }

            syncedCount++;
        }

        IReadOnlyList<string> sequencePlanIds =
            await ResolveSequencePlanIdsAsync(parent, cancellationToken);

        if (sequencePlanIds.Count > 0)
        {
            string? existingSequence = await artifactFileStore.TryReadAsync(
                parent.WorkspacePath,
                OrchiArtifactFileStore.PlanSequenceRelativePath,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(existingSequence))
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
        }

        if (syncedCount > 0)
        {
            logger.LogInformation(
                "Synced {PlanCount} plan file(s) for orchestration chat {ChatId}.",
                syncedCount,
                parent.Id);
        }
    }

    private async Task<IReadOnlyList<string>> ResolveSequencePlanIdsAsync(
        ChatSession parent,
        CancellationToken cancellationToken)
    {
        string? sequenceFile = await artifactFileStore.TryReadAsync(
            parent.WorkspacePath,
            OrchiArtifactFileStore.PlanSequenceRelativePath,
            cancellationToken);

        IReadOnlyList<string>? fromFile = TryParseSequenceFile(sequenceFile);
        if (fromFile is { Count: > 0 })
        {
            return fromFile;
        }

        return PlanSequenceMarkdownParser.ParseSequenceFromMessages(parent.Messages);
    }

    private static IReadOnlyList<string>? TryParseSequenceFile(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var ids = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in content.Split('\n'))
        {
            string trimmed = rawLine.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            string id = trimmed.ToLowerInvariant();
            if (!seen.Add(id))
            {
                continue;
            }

            ids.Add(id);
        }

        return ids.Count == 0 ? null : ids;
    }
}
