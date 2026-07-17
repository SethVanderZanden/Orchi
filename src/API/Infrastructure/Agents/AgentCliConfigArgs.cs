namespace Orchi.Api.Infrastructure.Agents;

/// <summary>
/// Shared helpers for emitting agent CLI config overrides.
/// Codex uses <c>-c key=value</c> (TOML values); other adapters can reuse or ignore.
/// </summary>
public static class AgentCliConfigArgs
{
    public static void AppendOverrides(
        ICollection<string> arguments,
        IReadOnlyDictionary<string, string>? overrides)
    {
        if (overrides is null || overrides.Count == 0)
        {
            return;
        }

        foreach (KeyValuePair<string, string> entry in overrides.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            AppendOverride(arguments, entry.Key, entry.Value);
        }
    }

    public static void AppendOverride(ICollection<string> arguments, string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        arguments.Add("-c");
        arguments.Add($"{key.Trim()}={FormatTomlValue(key.Trim(), value.Trim())}");
    }

    /// <summary>
    /// Formats a raw config value as TOML for Codex <c>-c</c>.
    /// Numeric keys / parseable integers stay bare; everything else is a quoted string.
    /// </summary>
    public static string FormatTomlValue(string key, string value)
    {
        if (IsNumericConfigKey(key) || long.TryParse(value, out _))
        {
            return value;
        }

        string escaped = value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

        return $"\"{escaped}\"";
    }

    private static bool IsNumericConfigKey(string key) =>
        string.Equals(key, "model_context_window", StringComparison.OrdinalIgnoreCase);
}
