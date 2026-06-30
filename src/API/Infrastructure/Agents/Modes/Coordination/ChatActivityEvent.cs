namespace Orchi.Api.Infrastructure.Agents.Modes.Coordination;

public enum ChatActivityKind
{
    ChildUserMessage,
    ChildMessageCompleted
}

public sealed record ChatActivityEvent(
    Guid ChildChatId,
    ChatMode ChildMode,
    ChatActivityKind Kind,
    string? LastMessageRole,
    string? LastMessageContent);
