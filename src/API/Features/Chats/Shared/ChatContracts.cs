using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.Shared;

public static class ChatMapper
{
    public static ChatSummaryResponse ToSummary(ChatSession session)
    {
        ChatMessage? lastMessage = session.Messages.LastOrDefault();

        return new ChatSummaryResponse(
            session.Id,
            DeriveTitle(session),
            lastMessage?.Content ?? "Start a conversation with Orchi",
            lastMessage?.CreatedAt ?? DateTimeOffset.UtcNow,
            session.AgentId,
            session.WorkspacePath);
    }

    public static ChatDetailResponse ToDetail(ChatSession session) =>
        new(
            session.Id,
            DeriveTitle(session),
            session.AgentId,
            session.WorkspacePath,
            session.Messages.Select(ToMessage).ToArray());

    public static ChatMessageResponse ToMessage(ChatMessage message) =>
        new(message.Id, message.Role, message.Content, message.CreatedAt, message.Status);

    private static string DeriveTitle(ChatSession session)
    {
        ChatMessage? firstUser = session.Messages.FirstOrDefault(message => message.Role == "user");
        if (firstUser is null)
        {
            return "New chat";
        }

        string trimmed = firstUser.Content.Trim();
        return trimmed.Length > 42 ? $"{trimmed[..42]}…" : trimmed;
    }
}

public sealed record ChatSummaryResponse(
    Guid Id,
    string Title,
    string Preview,
    DateTimeOffset UpdatedAt,
    string AgentId,
    string WorkspacePath);

public sealed record ChatDetailResponse(
    Guid Id,
    string Title,
    string AgentId,
    string WorkspacePath,
    IReadOnlyList<ChatMessageResponse> Messages);

public sealed record ChatMessageResponse(
    Guid Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    string Status);

public sealed record CreateChatRequest(
    string Agent,
    string WorkspacePath);

public sealed record SendMessageRequest(string Content);

public sealed record CreateChatResponse(
    Guid Id,
    string AgentId,
    string WorkspacePath);

public sealed record SendMessageDoneResponse(Guid MessageId);

public sealed record SseStatusPayload(string Phase);

public sealed record SseTokenPayload(string Text);

public sealed record SseToolPayload(string Label);

public sealed record SseErrorPayload(string Code, string Message);
