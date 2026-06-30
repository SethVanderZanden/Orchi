namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal static class CursorToolLabelFormatter
{
    private const string ToolCallSuffix = "ToolCall";

    private static readonly Dictionary<string, string> ToolLabels = new(StringComparer.Ordinal)
    {
        ["readToolCall"] = "Reading",
        ["writeToolCall"] = "Writing",
        ["listToolCall"] = "Listing",
        ["grepToolCall"] = "Searching",
        ["searchToolCall"] = "Searching",
        ["shellToolCall"] = "Running",
        ["bashToolCall"] = "Running"
    };

    public static string Format(string name, string status, string? detail)
    {
        string label = ToolLabels.TryGetValue(name, out string? verb) ? verb : FormatToolName(name);
        string statusLabel = status == "started" ? string.Empty : $" ({status})";

        if (!string.IsNullOrEmpty(detail))
        {
            return $"{label} {detail}{statusLabel}";
        }

        return $"{label}{statusLabel}";
    }

    private static string FormatToolName(string name)
    {
        if (name.EndsWith(ToolCallSuffix, StringComparison.Ordinal))
        {
            return name[..^ToolCallSuffix.Length];
        }

        return name;
    }
}
