using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal static class CursorModelListParser
{
    public static IReadOnlyList<AgentModelListEntry> Parse(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        var entries = new List<AgentModelListEntry>();

        foreach (string rawLine in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            AgentModelListEntry? entry = ParseLine(rawLine);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    internal static AgentModelListEntry? ParseLine(string rawLine)
    {
        string line = rawLine.Trim();
        if (line.Length == 0)
        {
            return null;
        }

        bool isDefault = false;
        bool isCurrent = false;

        while (TryStripSuffix(ref line, "(default)", ref isDefault)
               || TryStripSuffix(ref line, "(current)", ref isCurrent))
        {
        }

        string slug = line.Trim();
        if (slug.Length == 0)
        {
            return null;
        }

        return new AgentModelListEntry(slug, slug, isDefault, isCurrent);
    }

    private static bool TryStripSuffix(ref string line, string suffix, ref bool flag)
    {
        if (!line.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        flag = true;
        line = line[..^suffix.Length].TrimEnd();
        return true;
    }
}
