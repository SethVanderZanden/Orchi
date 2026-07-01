namespace Orchi.Api.Infrastructure.Agents.Modes;

public interface IConversationContextBuilder
{
    string BuildDynamicSuffix(ChatSession session, string currentUserContent, string? middleSection = null);
}

public sealed class ConversationContextBuilder : IConversationContextBuilder
{
    public string BuildDynamicSuffix(ChatSession session, string currentUserContent, string? middleSection = null)
    {
        var parts = new List<string>();

        string? history = ConversationHistoryFormatter.Format(session, currentUserContent);
        if (!string.IsNullOrWhiteSpace(history))
        {
            parts.Add(history);
        }

        if (!string.IsNullOrWhiteSpace(middleSection))
        {
            parts.Add(middleSection.Trim());
        }

        parts.Add(currentUserContent.Trim());
        return string.Join("\n\n", parts);
    }
}
