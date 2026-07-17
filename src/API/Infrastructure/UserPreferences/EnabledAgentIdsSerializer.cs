using System.Text.Json;

namespace Orchi.Api.Infrastructure.UserPreferences;

public static class EnabledAgentIdsSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IReadOnlyList<string> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        List<string>? parsed = TryDeserialize(json);
        if (parsed is null)
        {
            return [];
        }

        return Normalize(parsed);
    }

    public static string Serialize(IEnumerable<string> agentIds) =>
        JsonSerializer.Serialize(Normalize(agentIds), JsonOptions);

    public static IReadOnlyList<string> Normalize(IEnumerable<string> agentIds) =>
        agentIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

    private static List<string>? TryDeserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
