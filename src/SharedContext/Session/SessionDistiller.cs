using Orchi.SharedContext.Storage;

namespace Orchi.SharedContext.Session;

public interface ISessionDistiller
{
    Task DistillTurnAsync(
        string workspacePath,
        Guid chatId,
        string userContent,
        string assistantContent,
        CancellationToken cancellationToken);
}

internal sealed class SessionDistiller(IContextStore contextStore) : ISessionDistiller
{
    private const int MaxSummaryLength = 4000;

    public async Task DistillTurnAsync(
        string workspacePath,
        Guid chatId,
        string userContent,
        string assistantContent,
        CancellationToken cancellationToken)
    {
        string? existing = await contextStore.GetSessionSummaryAsync(workspacePath, chatId, cancellationToken);
        string distilled = BuildSummary(existing, userContent, assistantContent);

        await contextStore.UpsertSessionSummaryAsync(
            workspacePath,
            chatId,
            distilled,
            status: "active",
            cancellationToken);
    }

    private static string BuildSummary(string? existing, string userContent, string assistantContent)
    {
        var builder = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(existing))
        {
            builder.AppendLine(existing.Trim());
            builder.AppendLine();
        }

        builder.AppendLine($"- User: {Truncate(userContent, 500)}");
        builder.AppendLine($"- Assistant: {Truncate(assistantContent, 800)}");

        string result = builder.ToString().Trim();
        if (result.Length <= MaxSummaryLength)
        {
            return result;
        }

        return result[^MaxSummaryLength..];
    }

    private static string Truncate(string value, int maxLength)
    {
        string trimmed = value.Trim().Replace('\n', ' ');
        if (trimmed.Length <= maxLength)
        {
            return trimmed;
        }

        return trimmed[..maxLength] + "...";
    }
}
