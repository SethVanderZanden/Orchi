using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;

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
            session.WorkspacePath,
            ChatModeParser.ToApiString(session.Mode),
            session.ParentChatId,
            session.AttachedPlanId,
            session.GoalChatId);
    }

    public static ChatDetailResponse ToDetail(ChatSession session) =>
        new(
            session.Id,
            DeriveTitle(session),
            session.AgentId,
            session.WorkspacePath,
            ChatModeParser.ToApiString(session.Mode),
            session.ParentChatId,
            session.AttachedPlanId,
            session.GoalChatId,
            session.Messages.Select(ToMessage).ToArray());

    public static ChatMessageResponse ToMessage(ChatMessage message) =>
        new(message.Id, message.Role, message.Content, message.CreatedAt, message.Status);

    private static string DeriveTitle(ChatSession session)
    {
        ChatMessage? firstUser = session.Messages.FirstOrDefault(message => message.Role == "user");
        if (firstUser is null)
        {
            return session.Mode switch
            {
                ChatMode.Orchestrate => "Orchestration",
                ChatMode.Goal => "Goal tracker",
                ChatMode.Implement => "Implementation",
                _ => "New chat"
            };
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
    string WorkspacePath,
    string Mode,
    Guid? ParentChatId,
    Guid? AttachedPlanId,
    Guid? GoalChatId);

public sealed record ChatDetailResponse(
    Guid Id,
    string Title,
    string AgentId,
    string WorkspacePath,
    string Mode,
    Guid? ParentChatId,
    Guid? AttachedPlanId,
    Guid? GoalChatId,
    IReadOnlyList<ChatMessageResponse> Messages);

public sealed record ChatMessageResponse(
    Guid Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    string Status);

public sealed record UpdateChatRequest(string Mode, Guid? AttachedPlanId = null);

public sealed record CreateChatRequest(
    string Agent,
    string WorkspacePath,
    string? Mode = null,
    Guid? ParentChatId = null,
    Guid? AttachedPlanId = null);

public sealed record SendMessageRequest(string Content);

public sealed record CreateChatResponse(
    Guid Id,
    string AgentId,
    string WorkspacePath,
    string Mode,
    Guid? ParentChatId,
    Guid? AttachedPlanId,
    Guid? GoalChatId);

public sealed record SendMessageDoneResponse(Guid MessageId);

public sealed record SseStatusPayload(string Phase);

public sealed record SseTokenPayload(string Text);

public sealed record SseToolPayload(string Label);

public sealed record SseErrorPayload(string Code, string Message);

public sealed record CreatePlanRequest(string Title, string ContentMarkdown);

public sealed record UpdatePlanRequest(string? Title, string? ContentMarkdown, string? Status);

public sealed record PlanResponse(
    Guid Id,
    Guid SourceChatId,
    string Title,
    string ContentMarkdown,
    string Status,
    IReadOnlyList<SubPlanResponse> SubPlans);

public sealed record SubPlanResponse(
    Guid Id,
    string Title,
    string ContentMarkdown,
    Guid? AssignedChatId,
    string Status);

public sealed record DispatchSubPlanRequest(Guid SubPlanId, string ChildMode);

public sealed record DispatchSubPlanResponse(Guid ChildChatId, Guid SubPlanId);

public sealed record HandoffToGoalResponse(Guid GoalChatId);
