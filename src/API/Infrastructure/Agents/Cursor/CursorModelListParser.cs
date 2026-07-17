using System.Text.RegularExpressions;
using Orchi.Api.Infrastructure.Agents.Models;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal static partial class CursorModelListParser
{
    // Cursor tip/help lines (and ANSI-colored variants) must not become catalog entries.
    [GeneratedRegex(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.CultureInvariant)]
    private static partial Regex AnsiEscapeRegex();

    // Slugs: `claude-sonnet-4` or parameterized `claude-opus-4-8[context=1m,effort=high,fast=false]`.
    [GeneratedRegex(
        @"^[A-Za-z0-9][A-Za-z0-9._/-]*(?:\[[^\]]+\])?$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ModelSlugRegex();

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
        string line = StripAnsi(rawLine).Trim();
        if (line.Length == 0)
        {
            return null;
        }

        if (IsNonModelLine(line))
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
        if (slug.Length == 0 || !ModelSlugRegex().IsMatch(slug))
        {
            return null;
        }

        return new AgentModelListEntry(slug, slug, isDefault, isCurrent);
    }

    private static string StripAnsi(string value) => AnsiEscapeRegex().Replace(value, string.Empty);

    private static bool IsNonModelLine(string line)
    {
        if (line.StartsWith("Tip:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (line.StartsWith("Usage:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (line.StartsWith("Help:", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Help copy often embeds "--model" / "%2Fmodel" guidance rather than a slug.
        if (line.Contains("--model", StringComparison.OrdinalIgnoreCase)
            || line.Contains("%2Fmodel", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
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
