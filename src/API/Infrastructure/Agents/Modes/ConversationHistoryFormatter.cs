namespace Orchi.Api.Infrastructure.Agents.Modes;

internal static class ConversationHistoryFormatter
{
    public const int SafetyNetMessageCount = 10;
    public const int NoResumeMaxMessageCount = 50;

    public static string? Format(ChatSession session, string currentUserContent)
    {
        List<ChatMessage> prior = GetPriorMessages(session, currentUserContent);
        if (prior.Count == 0)
        {
            return null;
        }

        bool hasResume = !string.IsNullOrWhiteSpace(session.ExternalSessionId);
        int maxMessages = hasResume ? SafetyNetMessageCount : NoResumeMaxMessageCount;
        if (prior.Count > maxMessages)
        {
            prior = prior.TakeLast(maxMessages).ToList();
        }

        var lines = new List<string> { "## Conversation so far", string.Empty };
        foreach (ChatMessage message in prior)
        {
            if (string.IsNullOrWhiteSpace(message.Content) || IsInFlight(message))
            {
                continue;
            }

            string roleLabel = message.Role.Equals("user", StringComparison.OrdinalIgnoreCase)
                ? "User"
                : "Assistant";
            lines.Add($"{roleLabel}: {message.Content.Trim()}");
            lines.Add(string.Empty);
        }

        return lines.Count <= 2 ? null : string.Join('\n', lines).TrimEnd();
    }

    private static List<ChatMessage> GetPriorMessages(ChatSession session, string currentUserContent)
    {
        List<ChatMessage> messages;
        lock (session.Sync)
        {
            messages = session.Messages.ToList();
        }

        while (messages.Count > 0 &&
               messages[^1].Role == "assistant" &&
               IsInFlight(messages[^1]))
        {
            messages.RemoveAt(messages.Count - 1);
        }

        if (messages.Count > 0 &&
            messages[^1].Role == "user" &&
            messages[^1].Content.Trim() == currentUserContent.Trim())
        {
            messages.RemoveAt(messages.Count - 1);
        }

        return messages
            .Where(message => !IsInFlight(message) && !string.IsNullOrWhiteSpace(message.Content))
            .ToList();
    }

    private static bool IsInFlight(ChatMessage message) =>
        message.Status is "processing" or "streaming";
}
