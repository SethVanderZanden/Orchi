using System.Text.Json;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal static class CodexNdjsonParser
{
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
                case "thread.started":
                    foreach (AgentEvent started in ParseThreadStarted(root))
                    {
                        yield return started;
                    }

                    break;

                case "item.started":
                case "item.completed":
                    foreach (AgentEvent itemEvent in ParseItemEvent(root, type))
                    {
                        yield return itemEvent;
                    }

                    break;

                case "turn.completed":
                    yield return new AgentCompletedEvent(ExternalSessionId: null, FullText: string.Empty);
                    break;

                case "turn.failed":
                    yield return new AgentErrorEvent(
                        "Agent.TurnFailed",
                        ReadErrorMessage(root) ?? "Codex turn failed.");
                    break;

                case "error":
                    yield return new AgentErrorEvent(
                        "Agent.Error",
                        ReadErrorMessage(root) ?? "Codex reported an error.");
                    break;
            }
        }
    }

    private static IEnumerable<AgentEvent> ParseThreadStarted(JsonElement root)
    {
        if (!root.TryGetProperty("thread_id", out JsonElement threadIdElement))
        {
            yield break;
        }

        string? threadId = threadIdElement.GetString();
        if (string.IsNullOrWhiteSpace(threadId))
        {
            yield break;
        }

        yield return new AgentSessionStartedEvent(threadId);
    }

    private static IEnumerable<AgentEvent> ParseItemEvent(JsonElement root, string eventType)
    {
        if (!root.TryGetProperty("item", out JsonElement item))
        {
            yield break;
        }

        string itemType = ReadItemType(item);
        if (string.IsNullOrWhiteSpace(itemType))
        {
            yield break;
        }

        if (IsAgentMessage(itemType))
        {
            if (!string.Equals(eventType, "item.completed", StringComparison.Ordinal))
            {
                yield break;
            }

            string? text = ReadItemText(item);
            if (!string.IsNullOrEmpty(text))
            {
                yield return new AgentTextDeltaEvent(text);
            }

            yield break;
        }

        if (string.Equals(eventType, "item.started", StringComparison.Ordinal)
            && TryBuildToolLabel(itemType, item, out string label))
        {
            yield return new AgentToolEvent(label);
        }
    }

    private static string ReadItemType(JsonElement item)
    {
        if (item.TryGetProperty("type", out JsonElement typeElement))
        {
            return typeElement.GetString() ?? string.Empty;
        }

        if (item.TryGetProperty("item_type", out JsonElement legacyTypeElement))
        {
            return legacyTypeElement.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static bool IsAgentMessage(string itemType) =>
        string.Equals(itemType, "agent_message", StringComparison.OrdinalIgnoreCase)
        || string.Equals(itemType, "assistant_message", StringComparison.OrdinalIgnoreCase);

    private static string? ReadItemText(JsonElement item)
    {
        if (item.TryGetProperty("text", out JsonElement textElement))
        {
            return textElement.GetString();
        }

        return null;
    }

    private static bool TryBuildToolLabel(string itemType, JsonElement item, out string label)
    {
        label = itemType switch
        {
            "command_execution" => BuildCommandLabel(item),
            "file_change" => "Applying file changes",
            "mcp_tool_call" => BuildMcpLabel(item),
            "web_search" => "Web search",
            "todo_list" => "Updating todo list",
            _ => string.Empty
        };

        return !string.IsNullOrWhiteSpace(label);
    }

    private static string BuildCommandLabel(JsonElement item)
    {
        if (item.TryGetProperty("command", out JsonElement commandElement))
        {
            string? command = commandElement.GetString();
            if (!string.IsNullOrWhiteSpace(command))
            {
                return $"Running {Truncate(command.Trim(), 80)}";
            }
        }

        return "Running command";
    }

    private static string BuildMcpLabel(JsonElement item)
    {
        string? server = item.TryGetProperty("server", out JsonElement serverElement)
            ? serverElement.GetString()
            : null;
        string? tool = item.TryGetProperty("tool", out JsonElement toolElement)
            ? toolElement.GetString()
            : null;

        if (!string.IsNullOrWhiteSpace(server) && !string.IsNullOrWhiteSpace(tool))
        {
            return $"MCP {server}/{tool}";
        }

        if (!string.IsNullOrWhiteSpace(tool))
        {
            return $"MCP {tool}";
        }

        return "MCP tool";
    }

    private static string? ReadErrorMessage(JsonElement root)
    {
        if (root.TryGetProperty("message", out JsonElement messageElement))
        {
            return messageElement.GetString();
        }

        if (root.TryGetProperty("error", out JsonElement errorElement))
        {
            if (errorElement.ValueKind == JsonValueKind.String)
            {
                return errorElement.GetString();
            }

            if (errorElement.TryGetProperty("message", out JsonElement nestedMessage))
            {
                return nestedMessage.GetString();
            }
        }

        return null;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
