using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Plans;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;
using Orchi.Api.Infrastructure.Agents.Plans.Persistence;

namespace Orchi.Api.Infrastructure.Agents.Orchestration;

public interface IOrchestrationPlanSource
{
    Task<IReadOnlyList<PlanMarkdownParser.ParsedPlan>> ResolvePlansAsync(
        ChatSession parent,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ResolveSequencePlanIdsAsync(
        ChatSession parent,
        IReadOnlyList<string>? workflowSequencePlanIds,
        CancellationToken cancellationToken);

    Task<string?> ResolvePlanContentAsync(
        ChatSession parent,
        string planId,
        string? fallbackContentMarkdown,
        CancellationToken cancellationToken);
}

public sealed class OrchestrationPlanSource(
    IPlanStore planStore,
    OrchiArtifactFileStore artifactFileStore) : IOrchestrationPlanSource
{
    public async Task<IReadOnlyList<PlanMarkdownParser.ParsedPlan>> ResolvePlansAsync(
        ChatSession parent,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PlanMarkdownParser.ParsedPlan> fromMessages =
            string.IsNullOrWhiteSpace(parent.WorkspacePath)
                ? PlanMarkdownParser.ExtractAllPlansFromMessages(parent.Messages)
                : await PlanMarkdownParser.HydratePlansFromWorkspaceAsync(
                    parent.WorkspacePath,
                    parent.Messages,
                    artifactFileStore,
                    cancellationToken);

        if (!IsOrchestrationParent(parent))
        {
            return fromMessages;
        }

        IReadOnlyList<StoredPlan> storedPlans =
            await planStore.ListBySourceChatAsync(parent.Id, cancellationToken);

        if (storedPlans.Count == 0)
        {
            return fromMessages;
        }

        var merged = new Dictionary<string, PlanMarkdownParser.ParsedPlan>(StringComparer.OrdinalIgnoreCase);

        foreach (StoredPlan storedPlan in storedPlans)
        {
            string relativePath = PlanMarkdownParser.BuildConventionalPlanFilePath(storedPlan.PlanId);
            string content = await ReadPlanContentAsync(
                parent.WorkspacePath,
                relativePath,
                storedPlan.ContentMarkdown,
                cancellationToken);

            merged[storedPlan.PlanId] = new PlanMarkdownParser.ParsedPlan(
                storedPlan.PlanId,
                storedPlan.Title,
                content,
                relativePath);
        }

        foreach (PlanMarkdownParser.ParsedPlan messagePlan in fromMessages)
        {
            if (merged.TryGetValue(messagePlan.PlanId, out PlanMarkdownParser.ParsedPlan? existing))
            {
                if (!string.IsNullOrWhiteSpace(messagePlan.ContentMarkdown))
                {
                    merged[messagePlan.PlanId] = messagePlan with
                    {
                        PlanFilePath = messagePlan.PlanFilePath ?? existing.PlanFilePath
                    };
                }

                continue;
            }

            merged[messagePlan.PlanId] = messagePlan;
        }

        return merged.Values.ToArray();
    }

    public async Task<IReadOnlyList<string>> ResolveSequencePlanIdsAsync(
        ChatSession parent,
        IReadOnlyList<string>? workflowSequencePlanIds,
        CancellationToken cancellationToken)
    {
        if (workflowSequencePlanIds is { Count: > 0 })
        {
            return workflowSequencePlanIds;
        }

        if (!string.IsNullOrWhiteSpace(parent.WorkspacePath))
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
        }

        return PlanSequenceMarkdownParser.ParseSequenceFromMessages(parent.Messages);
    }

    public async Task<string?> ResolvePlanContentAsync(
        ChatSession parent,
        string planId,
        string? fallbackContentMarkdown,
        CancellationToken cancellationToken)
    {
        string relativePath = PlanMarkdownParser.BuildConventionalPlanFilePath(planId);

        if (!string.IsNullOrWhiteSpace(parent.WorkspacePath))
        {
            string? fileContent = await artifactFileStore.TryReadAsync(
                parent.WorkspacePath,
                relativePath,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(fileContent))
            {
                return fileContent;
            }
        }

        StoredPlan? storedPlan = await planStore.GetAsync(parent.Id, planId, cancellationToken);
        if (storedPlan is not null)
        {
            return await ReadPlanContentAsync(
                parent.WorkspacePath,
                relativePath,
                storedPlan.ContentMarkdown,
                cancellationToken);
        }

        string? fromMessages = PlanMarkdownParser.TryExtractPlanFromMessages(parent.Messages, planId);
        if (!string.IsNullOrWhiteSpace(fromMessages))
        {
            return fromMessages;
        }

        return string.IsNullOrWhiteSpace(fallbackContentMarkdown) ? null : fallbackContentMarkdown;
    }

    private async Task<string> ReadPlanContentAsync(
        string workspacePath,
        string relativePath,
        string storedContent,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return storedContent;
        }

        string? fileContent = await artifactFileStore.TryReadAsync(
            workspacePath,
            relativePath,
            cancellationToken);

        return string.IsNullOrWhiteSpace(fileContent) ? storedContent : fileContent;
    }

    private static bool IsOrchestrationParent(ChatSession parent) =>
        string.Equals(parent.Mode, OrchestrationAgentModeStrategy.Mode, StringComparison.OrdinalIgnoreCase) &&
        parent.ParentChatId is null;

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
