using System.Text.RegularExpressions;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Infrastructure.Agents.Plans;

public static partial class PlanMarkdownParser
{
    [GeneratedRegex(
        @"<!--\s*orchi-plan:([a-z0-9]+(?:-[a-z0-9]+)*)\s*-->\s*([\s\S]*?)<!--\s*/orchi-plan\s*-->",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlanBlockPattern();

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

    [GeneratedRegex(
        @"(?:^|[\\/])plan-([a-z0-9]+(?:-[a-z0-9]+)*)\.md$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlanFilePathPattern();
}
