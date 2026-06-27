using System.Text.Json;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal static class CursorNdjsonParser
{
    private static readonly string[] DetailKeys =
    [
        "path",
        "command",
        "pattern",
        "query",
        "targetDirectory",
        "glob",
        "globPattern",
        "relativePath"
    ];

    public static IEnumerable<AgentEvent> ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            yield break;
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(line);
        }
        catch (JsonException)
        {
            yield break;
        }

        using (document)
        {
            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("type", out JsonElement typeElement))
            {
                yield break;
            }

            string type = typeElement.GetString() ?? string.Empty;

            switch (type)
            {
                case "assistant":
                    foreach (AgentEvent delta in ParseAssistantEvent(root))
                    {
                        yield return delta;
                    }

                    break;

                case "result":
                    foreach (AgentEvent completed in ParseResultEvent(root))
                    {
                        yield return completed;
                    }

                    break;

                case "tool_call":
                    IEnumerable<AgentEvent> toolEvents;
                    try
                    {
                        toolEvents = ParseToolEvent(root).ToArray();
                    }
                    catch
                    {
                        yield break;
                    }

                    foreach (AgentEvent tool in toolEvents)
                    {
                        yield return tool;
                    }

                    break;
            }
        }
    }

    private static IEnumerable<AgentEvent> ParseAssistantEvent(JsonElement root)
    {
        if (root.TryGetProperty("model_call_id", out _))
        {
            yield break;
        }

        bool hasTimestamp = root.TryGetProperty("timestamp_ms", out _);
        if (!hasTimestamp)
        {
            foreach (AgentEvent segment in ParseAssistantContent(root))
            {
                yield return segment;
            }

            yield break;
        }

        foreach (AgentEvent delta in ParseAssistantContent(root))
        {
            yield return delta;
        }
    }

    private static IEnumerable<AgentEvent> ParseAssistantContent(JsonElement root)
    {
        if (!root.TryGetProperty("message", out JsonElement messageElement))
        {
            yield break;
        }

        if (!messageElement.TryGetProperty("content", out JsonElement contentElement))
        {
            yield break;
        }

        foreach (JsonElement part in contentElement.EnumerateArray())
        {
            if (part.TryGetProperty("text", out JsonElement textElement))
            {
                string? text = textElement.GetString();
                if (!string.IsNullOrEmpty(text))
                {
                    yield return new AgentTextDeltaEvent(text);
                }
            }
        }
    }

    private static IEnumerable<AgentEvent> ParseResultEvent(JsonElement root)
    {
        string? externalSessionId = null;
        if (root.TryGetProperty("session_id", out JsonElement sessionIdElement))
        {
            externalSessionId = sessionIdElement.GetString();
        }

        string fullText = string.Empty;
        if (root.TryGetProperty("result", out JsonElement resultElement))
        {
            fullText = resultElement.GetString() ?? string.Empty;
        }

        yield return new AgentCompletedEvent(externalSessionId, fullText);
    }

    private static IEnumerable<AgentEvent> ParseToolEvent(JsonElement root)
    {
        string subtype = root.TryGetProperty("subtype", out JsonElement subtypeElement)
            ? subtypeElement.GetString() ?? string.Empty
            : string.Empty;

        string status = subtype switch
        {
            "started" => "started",
            "completed" => "completed",
            _ => subtype
        };

        if (string.IsNullOrEmpty(status))
        {
            yield break;
        }

        if (!root.TryGetProperty("tool_call", out JsonElement toolCallElement) ||
            toolCallElement.ValueKind != JsonValueKind.Object)
        {
            yield break;
        }

        foreach (JsonProperty property in toolCallElement.EnumerateObject())
        {
            if (property.Name.Equals("function", StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.Object)
            {
                string name = TryGetStringProperty(property.Value, "name") ?? "function";
                string? detail = ReadFunctionArgumentsDetail(property.Value);
                yield return new AgentToolEvent(name, status, detail);
                continue;
            }

            string toolName = property.Name;
            string? toolDetail = TryReadToolDetail(property.Value);
            yield return new AgentToolEvent(toolName, status, toolDetail);
        }
    }

    private static string? ReadFunctionArgumentsDetail(JsonElement functionElement)
    {
        string? arguments = TryGetStringProperty(functionElement, "arguments");
        if (string.IsNullOrWhiteSpace(arguments))
        {
            return null;
        }

        return TruncateDetail(arguments);
    }

    private static string? TryReadToolDetail(JsonElement toolElement)
    {
        if (toolElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (string key in DetailKeys)
        {
            string? directValue = TryGetStringProperty(toolElement, key);
            if (!string.IsNullOrEmpty(directValue))
            {
                return directValue;
            }
        }

        if (toolElement.TryGetProperty("args", out JsonElement argsElement))
        {
            string? argsDetail = ReadArgsDetail(argsElement);
            if (!string.IsNullOrEmpty(argsDetail))
            {
                return argsDetail;
            }
        }

        if (toolElement.TryGetProperty("result", out JsonElement resultElement))
        {
            return ReadResultDetail(resultElement);
        }

        return null;
    }

    private static string? ReadArgsDetail(JsonElement argsElement) =>
        argsElement.ValueKind switch
        {
            JsonValueKind.Object => ReadObjectDetailKeys(argsElement),
            JsonValueKind.Array => JoinArrayElements(argsElement),
            JsonValueKind.String => argsElement.GetString(),
            _ => null
        };

    private static string? ReadObjectDetailKeys(JsonElement objectElement)
    {
        foreach (string key in DetailKeys)
        {
            string? value = TryGetStringProperty(objectElement, key);
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        string? fileText = TryGetStringProperty(objectElement, "fileText");
        if (!string.IsNullOrEmpty(fileText))
        {
            return TruncateDetail(fileText);
        }

        return null;
    }

    private static string? ReadResultDetail(JsonElement resultElement)
    {
        if (resultElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (resultElement.TryGetProperty("success", out JsonElement successElement) &&
            successElement.ValueKind == JsonValueKind.Object)
        {
            foreach (string key in DetailKeys)
            {
                string? value = TryGetStringProperty(successElement, key);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
        }

        return ReadObjectDetailKeys(resultElement);
    }

    private static string? TryGetStringProperty(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object ||
            !element.TryGetProperty(propertyName, out JsonElement valueElement))
        {
            return null;
        }

        return valueElement.ValueKind switch
        {
            JsonValueKind.String => valueElement.GetString(),
            JsonValueKind.Number => valueElement.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }

    private static string? JoinArrayElements(JsonElement arrayElement)
    {
        var parts = new List<string>();

        foreach (JsonElement item in arrayElement.EnumerateArray())
        {
            string? part = item.ValueKind switch
            {
                JsonValueKind.String => item.GetString(),
                JsonValueKind.Number => item.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(part))
            {
                parts.Add(part);
            }
        }

        return parts.Count == 0 ? null : string.Join(' ', parts);
    }

    private static string TruncateDetail(string value, int maxLength = 120) =>
        value.Length <= maxLength ? value : value[..maxLength] + "...";
}
