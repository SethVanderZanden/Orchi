using System.Text.RegularExpressions;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;
using Orchi.Api.Infrastructure.Agents.Plans.Persistence;

namespace Orchi.Api.Infrastructure.Agents.Plans;

public sealed partial class PlanMaterializer(
    IPlanStore planStore,
    IOrchiArtifactWriterFactory artifactWriterFactory,
    ILogger<PlanMaterializer> logger) : IPlanMaterializer
{
    public async Task<IReadOnlyList<StoredPlan>> MaterializeAsync(
        ChatSession orchestrationChat,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orchestrationChat.WorkspacePath))
        {
            logger.LogWarning(
                "Skipping plan materialization for chat {ChatId}: workspace path is missing.",
                orchestrationChat.Id);
            return [];
        }

        IOrchiArtifactWriterStrategy planWriter = artifactWriterFactory.GetStrategy(OrchiArtifactKind.Plan);
        IReadOnlyList<PlanMarkdownParser.ParsedPlan> latestMessagePlans =
            ExtractPlansFromLatestAssistantMessage(orchestrationChat.Messages);

        IReadOnlyList<PlanMarkdownParser.ParsedPlan> plansToPersist = latestMessagePlans.Count > 0
            ? latestMessagePlans
            : ReadPlanFilesFromWorkspace(orchestrationChat.WorkspacePath, planWriter);

        if (plansToPersist.Count == 0)
        {
            return await planStore.ListBySourceChatAsync(orchestrationChat.Id, cancellationToken);
        }

        var persisted = new List<StoredPlan>(plansToPersist.Count);

        foreach (PlanMarkdownParser.ParsedPlan plan in plansToPersist)
        {
            try
            {
                await planStore.UpsertAsync(
                    new PlanUpsertModel(
                        plan.PlanId,
                        orchestrationChat.Id,
                        plan.Title,
                        plan.ContentMarkdown),
                    cancellationToken);

                await planWriter.WriteAsync(
                    orchestrationChat.WorkspacePath,
                    plan.PlanId,
                    plan.ContentMarkdown,
                    cancellationToken);

                StoredPlan? stored = await planStore.GetAsync(
                    orchestrationChat.Id,
                    plan.PlanId,
                    cancellationToken);

                if (stored is not null)
                {
                    persisted.Add(stored);
                }
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(
                    ex,
                    "Failed to materialize plan {PlanId} for chat {ChatId}.",
                    plan.PlanId,
                    orchestrationChat.Id);
            }
        }

        return persisted.Count > 0
            ? persisted
            : await planStore.ListBySourceChatAsync(orchestrationChat.Id, cancellationToken);
    }

    private static IReadOnlyList<PlanMarkdownParser.ParsedPlan> ExtractPlansFromLatestAssistantMessage(
        IReadOnlyList<ChatMessage> messages)
    {
        for (int index = messages.Count - 1; index >= 0; index--)
        {
            ChatMessage message = messages[index];
            if (!string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            IReadOnlyList<PlanMarkdownParser.ParsedPlan> plans =
                PlanMarkdownParser.ExtractPlans(message.Content);
            if (plans.Count > 0)
            {
                return plans;
            }

            // Walk past status-only assistant messages (e.g. "Started implementation…").
        }

        return [];
    }

    private static IReadOnlyList<PlanMarkdownParser.ParsedPlan> ReadPlanFilesFromWorkspace(
        string workspacePath,
        IOrchiArtifactWriterStrategy planWriter)
    {
        string orchiDirectory = Path.Combine(workspacePath, ".orchi");
        if (!Directory.Exists(orchiDirectory))
        {
            return [];
        }

        var plans = new List<PlanMarkdownParser.ParsedPlan>();

        foreach (string filePath in Directory.EnumerateFiles(orchiDirectory, "plan-*.md"))
        {
            string relativePath = Path.GetRelativePath(workspacePath, filePath).Replace('\\', '/');
            string? planId = PlanMarkdownParser.TryExtractPlanIdFromPath(relativePath);
            if (planId is null)
            {
                continue;
            }

            // Ignore paths that do not match the canonical writer template.
            if (!string.Equals(
                    relativePath,
                    planWriter.BuildRelativePath(planId),
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string content = File.ReadAllText(filePath).Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            plans.Add(new PlanMarkdownParser.ParsedPlan(planId, ExtractTitle(content), content));
        }

        return plans;
    }

    private static string ExtractTitle(string content)
    {
        Match headingMatch = TitlePattern().Match(content);
        return headingMatch.Success ? headingMatch.Groups[1].Value.Trim() : "Untitled plan";
    }

    [GeneratedRegex(@"^#\s+(.+)$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex TitlePattern();
}
