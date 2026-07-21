using System.Text.Json;
using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal sealed class CodexNdjsonParser : IAgentStreamLineParser
{
    private readonly Dictionary<string, string> _agentMessageTextByItemId = new(StringComparer.Ordinal);

    public IEnumerable<AgentEvent> ParseLine(string line)
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
                case "item.updated":
                case "item.completed":
                    foreach (AgentEvent itemEvent in ParseItemEvent(root, type))
                    {
                        yield return itemEvent;
                    }

                    break;

                case "turn.started":
                    yield return new AgentToolEvent("Working…");
                    break;

                case "turn.completed":
                    yield return new AgentCompletedEvent(ExternalSessionId: null, FullText: string.Empty);
                    break;

                case "turn.failed":
                    yield return new AgentErrorEvent(
                        "Agent.TurnFailed",
                        ReadErrorMessage(root) ?? "Codex turn failed.");
                    break;

                // Transient reconnect/status errors; turn.failed carries the final failure.
                case "error":
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

    private IEnumerable<AgentEvent> ParseItemEvent(JsonElement root, string eventType)
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
            if (string.Equals(eventType, "item.started", StringComparison.Ordinal))
            {
                yield break;
            }

            foreach (AgentEvent textEvent in EmitAgentMessageText(item))
            {
                yield return textEvent;
            }

            yield break;
        }

        if (IsReasoning(itemType))
        {
            if (string.Equals(eventType, "item.started", StringComparison.Ordinal)
                || string.Equals(eventType, "item.updated", StringComparison.Ordinal))
            {
                yield return new AgentToolEvent("Thinking…");
            }

            yield break;
        }

        if (string.Equals(eventType, "item.started", StringComparison.Ordinal)
            && TryBuildToolLabel(itemType, item, out string label))
        {
            yield return new AgentToolEvent(label);
        }
    }

    private IEnumerable<AgentEvent> EmitAgentMessageText(JsonElement item)
    {
        string? itemId = item.TryGetProperty("id", out JsonElement idElement)
            ? idElement.GetString()
            : null;
        string? text = ReadItemText(item);
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        string key = string.IsNullOrWhiteSpace(itemId) ? string.Empty : itemId;
        _agentMessageTextByItemId.TryGetValue(key, out string? previousText);
        previousText ??= string.Empty;

        if (text.Length <= previousText.Length
            && string.Equals(text, previousText, StringComparison.Ordinal))
        {
            yield break;
        }

        string delta = text.StartsWith(previousText, StringComparison.Ordinal)
            ? text[previousText.Length..]
            : text;

        if (!string.IsNullOrEmpty(delta))
        {
            yield return new AgentTextDeltaEvent(delta);
        }

        _agentMessageTextByItemId[key] = text;
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

    private static bool IsReasoning(string itemType) =>
        string.Equals(itemType, "reasoning", StringComparison.OrdinalIgnoreCase);

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
            "collab_tool_call" => BuildCollabLabel(item),
            "web_search" => BuildWebSearchLabel(item),
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

    private static string BuildCollabLabel(JsonElement item)
    {
        if (item.TryGetProperty("tool", out JsonElement toolElement))
        {
            string? tool = toolElement.GetString();
            if (!string.IsNullOrWhiteSpace(tool))
            {
                return $"Collab {tool.Replace("_", " ", StringComparison.Ordinal)}";
            }
        }

        return "Collab tool";
    }

    private static string BuildWebSearchLabel(JsonElement item)
    {
        if (item.TryGetProperty("query", out JsonElement queryElement))
        {
            string? query = queryElement.GetString();
            if (!string.IsNullOrWhiteSpace(query))
            {
                return $"Web search: {Truncate(query.Trim(), 80)}";
            }
        }

        return "Web search";
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
