namespace Orchi.SharedContext.Prompts;

public sealed record PriorChatMessage(string Role, string Content, string Status);

public sealed record PromptSessionContext(
    string WorkspacePath,
    string ModeKey,
    Guid ChatId,
    string? ExternalSessionId,
    string? PreviousModeKey,
    DateTimeOffset? ModeChangedAt,
    IReadOnlyList<PriorChatMessage> PriorMessages,
    string CurrentUserContent,
    string? MiddleSection = null,
    bool ForceAskCliProfile = false);

public sealed record PromptBuildResult(string StablePrefix, string DynamicContext);

public interface IPromptBuilder
{
    string BuildStablePrefix(string workspacePath, string modeInstructions);

    Task<PromptBuildResult> BuildTurnAsync(
        PromptSessionContext context,
        string modeInstructions,
        CancellationToken cancellationToken);
}
