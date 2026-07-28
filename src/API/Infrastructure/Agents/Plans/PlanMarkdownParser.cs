using System.Text.RegularExpressions;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;

namespace Orchi.Api.Infrastructure.Agents.Plans;

public static partial class PlanMarkdownParser
{
    public sealed record ParsedPlan(
        string PlanId,
        string Title,
        string ContentMarkdown,
        string? PlanFilePath = null);

    [GeneratedRegex(
        @"<!--\s*orchi-plan:([a-z0-9]+(?:-[a-z0-9]+)*)\s*-->\s*([\s\S]*?)<!--\s*/orchi-plan\s*-->",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlanBlockPattern();

    public static string BuildConventionalPlanFilePath(string planId)
    {
        string sanitizedPlanId = OrchiArtifactFileStore.SanitizePlanId(planId);
        return $".orchi/plan-{sanitizedPlanId}.md";
    }

    public static string? TryExtractPlanContent(string content, string planId)
    {
        if (string.IsNullOrWhiteSpace(content) || string.IsNullOrWhiteSpace(planId))
        {
            return null;
        }

        string normalizedPlanId = planId.Trim().ToLowerInvariant();

        foreach (Match match in PlanBlockPattern().Matches(content))
        {
            if (!string.Equals(match.Groups[1].Value, normalizedPlanId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string body = match.Groups[2].Value.Trim();
            if (TryResolvePlanFileReference(body, match.Groups[1].Value, out _))
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(body) ? null : body;
        }

        return null;
    }

    public static string? TryExtractPlanFromMessages(IEnumerable<ChatMessage> messages, string planId)
    {
        foreach (ChatMessage message in messages.Reverse())
        {
            if (!string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? planContent = TryExtractPlanContent(message.Content, planId);
            if (planContent is not null)
            {
                return planContent;
            }
        }

        return null;
    }

    public static string? TryExtractPlanIdFromPath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        string normalized = relativePath.Replace('\\', '/');
        Match match = PlanFilePathPattern().Match(normalized);
        return match.Success ? match.Groups[1].Value : null;
    }

    public static string? TryExtractReviewPlanIdFromPath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        string normalized = relativePath.Replace('\\', '/');
        Match match = ReviewFilePathPattern().Match(normalized);
        return match.Success ? match.Groups[1].Value : null;
    }

    public static string? TryExtractAnyPlanIdFromPath(string? relativePath) =>
        TryExtractPlanIdFromPath(relativePath) ?? TryExtractReviewPlanIdFromPath(relativePath);

    public static IReadOnlyList<ParsedPlan> ExtractPlans(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return [];
        }

        var plans = new Dictionary<string, ParsedPlan>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in PlanBlockPattern().Matches(content))
        {
            string planId = match.Groups[1].Value;
            string body = match.Groups[2].Value.Trim();

            if (TryResolvePlanFileReference(body, planId, out string? planFilePath))
            {
                plans[planId] = new ParsedPlan(
                    planId,
                    "Untitled plan",
                    string.Empty,
                    planFilePath);
                continue;
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                continue;
            }

            plans[planId] = new ParsedPlan(planId, ExtractTitle(body), body);
        }

        return plans.Values.ToArray();
    }

    public static IReadOnlyList<ParsedPlan> ExtractAllPlansFromMessages(IEnumerable<ChatMessage> messages)
    {
        var plans = new Dictionary<string, ParsedPlan>(StringComparer.OrdinalIgnoreCase);

        foreach (ChatMessage message in messages)
        {
            if (!string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (ParsedPlan plan in ExtractPlans(message.Content))
            {
                plans[plan.PlanId] = plan;
            }
        }

        return plans.Values.ToArray();
    }

    public static async Task<IReadOnlyList<ParsedPlan>> DiscoverPlansFromWorkspaceAsync(
        string workspacePath,
        OrchiArtifactFileStore artifactFileStore,
        CancellationToken cancellationToken)
    {
        string orchiDirectory = Path.Combine(workspacePath, ".orchi");
        if (!Directory.Exists(orchiDirectory))
        {
            return [];
        }

        var plans = new List<ParsedPlan>();

        foreach (string filePath in Directory.EnumerateFiles(orchiDirectory, "plan-*.md"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            string fileName = Path.GetFileName(filePath);
            string relativePath = $".orchi/{fileName}";
            string? planId = TryExtractPlanIdFromPath(relativePath);
            if (planId is null)
            {
                continue;
            }

            string? content = await artifactFileStore.TryReadAsync(
                workspacePath,
                relativePath,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            plans.Add(new ParsedPlan(
                planId,
                ExtractTitle(content),
                content,
                relativePath));
        }

        return plans;
    }

    public static async Task<IReadOnlyList<ParsedPlan>> ResolvePlansFromWorkspaceAndMessagesAsync(
        string workspacePath,
        IEnumerable<ChatMessage> messages,
        OrchiArtifactFileStore artifactFileStore,
        CancellationToken cancellationToken)
    {
        var merged = new Dictionary<string, ParsedPlan>(StringComparer.OrdinalIgnoreCase);

        foreach (ParsedPlan inlinePlan in ExtractAllPlansFromMessages(messages))
        {
            if (inlinePlan.PlanFilePath is null && !string.IsNullOrWhiteSpace(inlinePlan.ContentMarkdown))
            {
                merged[inlinePlan.PlanId] = inlinePlan;
            }
        }

        IReadOnlyList<ParsedPlan> referencedPlans = ExtractAllPlansFromMessages(messages)
            .Where(plan => plan.PlanFilePath is not null)
            .ToArray();

        foreach (ParsedPlan referencedPlan in referencedPlans)
        {
            if (merged.ContainsKey(referencedPlan.PlanId))
            {
                continue;
            }

            ParsedPlan hydrated = await HydratePlanFromWorkspaceAsync(
                workspacePath,
                referencedPlan,
                artifactFileStore,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(hydrated.ContentMarkdown))
            {
                merged[hydrated.PlanId] = hydrated;
            }
        }

        return merged.Values.ToArray();
    }

    public static async Task<IReadOnlyList<ParsedPlan>> HydratePlansFromWorkspaceAsync(
        string workspacePath,
        IEnumerable<ChatMessage> messages,
        OrchiArtifactFileStore artifactFileStore,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ParsedPlan> plans = ExtractAllPlansFromMessages(messages);
        if (plans.Count == 0)
        {
            return plans;
        }

        var hydrated = new List<ParsedPlan>(plans.Count);

        foreach (ParsedPlan plan in plans)
        {
            hydrated.Add(await HydratePlanFromWorkspaceAsync(
                workspacePath,
                plan,
                artifactFileStore,
                cancellationToken));
        }

        return hydrated;
    }

    public static async Task<ParsedPlan> HydratePlanFromWorkspaceAsync(
        string workspacePath,
        ParsedPlan plan,
        OrchiArtifactFileStore artifactFileStore,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(plan.ContentMarkdown) && plan.PlanFilePath is null)
        {
            return plan;
        }

        string relativePath = plan.PlanFilePath ?? BuildConventionalPlanFilePath(plan.PlanId);
        string? fileContent = await artifactFileStore.TryReadAsync(
            workspacePath,
            relativePath,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(fileContent))
        {
            return plan with { PlanFilePath = relativePath };
        }

        return plan with
        {
            PlanFilePath = relativePath,
            ContentMarkdown = fileContent,
            Title = ExtractTitle(fileContent)
        };
    }

    public static bool TryResolvePlanFileReference(string body, string planId, out string relativePath)
    {
        string trimmed = body.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            relativePath = BuildConventionalPlanFilePath(planId);
            return true;
        }

        if (trimmed.StartsWith("# ", StringComparison.Ordinal))
        {
            relativePath = string.Empty;
            return false;
        }

        string firstLine = trimmed.Split('\n', 2)[0].Trim().Trim('`');
        if (IsPlanFilePath(firstLine))
        {
            relativePath = NormalizePlanFilePath(firstLine);
            return true;
        }

        relativePath = string.Empty;
        return false;
    }

    private static bool IsPlanFilePath(string value) =>
        value.StartsWith(".orchi/", StringComparison.Ordinal) &&
        value.EndsWith(".md", StringComparison.OrdinalIgnoreCase);

    private static string NormalizePlanFilePath(string value) =>
        value.Replace('\\', '/').TrimStart('/');

    private static string ExtractTitle(string content)
    {
        Match headingMatch = TitlePattern().Match(content);
        return headingMatch.Success ? headingMatch.Groups[1].Value.Trim() : "Untitled plan";
    }

    [GeneratedRegex(@"^#\s+(.+)$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex TitlePattern();

    [GeneratedRegex(
        @"(?:^|[\\/])plan-([a-z0-9]+(?:-[a-z0-9]+)*)\.md$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlanFilePathPattern();

    [GeneratedRegex(
        @"(?:^|[\\/])review-([a-z0-9]+(?:-[a-z0-9]+)*)\.md$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReviewFilePathPattern();
}
