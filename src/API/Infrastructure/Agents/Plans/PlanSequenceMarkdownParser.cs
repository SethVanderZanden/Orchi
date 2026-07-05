using System.Text.RegularExpressions;

namespace Orchi.Api.Infrastructure.Agents.Plans;

public static partial class PlanSequenceMarkdownParser
{
    [GeneratedRegex(
        @"<!--\s*orchi-plan-sequence\s*-->\s*([\s\S]*?)<!--\s*\/orchi-plan-sequence\s*-->",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlanSequencePattern();

    [GeneratedRegex(
        @"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlanIdPattern();

    public static IReadOnlyList<string>? TryParseSequence(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        IReadOnlyList<string>? latest = null;
        bool found = false;

        foreach (Match match in PlanSequencePattern().Matches(content))
        {
            found = true;
            latest = ParseSequenceBody(match.Groups[1].Value);
        }

        return found ? latest ?? [] : null;
    }

    public static IReadOnlyList<string> ParseSequenceFromMessages(IEnumerable<ChatMessage> messages)
    {
        IReadOnlyList<string> latest = [];

        foreach (ChatMessage message in messages)
        {
            if (!string.Equals(message.Role, "assistant", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            IReadOnlyList<string>? parsed = TryParseSequence(message.Content);
            if (parsed is not null)
            {
                latest = parsed;
            }
        }

        return latest;
    }

    private static IReadOnlyList<string> ParseSequenceBody(string body)
    {
        var ids = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in body.Split('\n'))
        {
            string trimmed = rawLine.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            if (trimmed.StartsWith("- ", StringComparison.Ordinal))
            {
                trimmed = trimmed[2..].Trim();
            }

            string id = trimmed.ToLowerInvariant();
            if (!PlanIdPattern().IsMatch(id) || !seen.Add(id))
            {
                continue;
            }

            ids.Add(id);
        }

        return ids;
    }
}
