using System.Text.RegularExpressions;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Features.Chats.Shared;

public static partial class ChatMapper
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
            session.ProjectId,
            session.WorkspaceId,
            session.WorkspacePath,
            session.Mode,
            session.ModelId,
            session.ParentChatId,
            session.PlanFilePath,
            session.Status,
            session.LastReadAt);
    }

    public static ChatDetailResponse ToDetail(ChatSession session) =>
        new(
            session.Id,
            DeriveTitle(session),
            session.AgentId,
            session.ProjectId,
            session.WorkspaceId,
            session.WorkspacePath,
            session.Mode,
            session.ModelId,
            session.ParentChatId,
            session.PlanFilePath,
            session.Status,
            session.LastReadAt,
            session.Messages.Select(ToMessage).ToArray());

    public static ChatMessageResponse ToMessage(ChatMessage message) =>
        new(message.Id, message.Role, message.Content, message.CreatedAt, message.Status);

    private static string DeriveTitle(ChatSession session)
    {
        string? planTitle = DeriveTitleFromPlanFilePath(session.PlanFilePath);
        if (planTitle is not null)
        {
            return planTitle;
        }

        ChatMessage? firstUser = session.Messages.FirstOrDefault(message => message.Role == "user");
        if (firstUser is null)
        {
            return "New chat";
        }

        string trimmed = firstUser.Content.Trim();
        return trimmed.Length > 42 ? $"{trimmed[..42]}…" : trimmed;
    }

    private static string? DeriveTitleFromPlanFilePath(string? planFilePath)
    {
        if (string.IsNullOrWhiteSpace(planFilePath))
        {
            return null;
        }

        string normalized = planFilePath.Replace('\\', '/');
        Match reviewMatch = ReviewFilePathPattern().Match(normalized);
        if (reviewMatch.Success)
        {
            return $"{FormatPlanIdAsTitle(reviewMatch.Groups[1].Value)} review";
        }

        Match planMatch = PlanFilePathPattern().Match(normalized);
        if (!planMatch.Success)
        {
            return null;
        }

        return FormatPlanIdAsTitle(planMatch.Groups[1].Value);
    }

    private static string FormatPlanIdAsTitle(string planId)
    {
        string[] words = planId.Split('-', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return planId;
        }

        words[0] = char.ToUpperInvariant(words[0][0]) + words[0][1..];
        return string.Join(' ', words);
    }

    [GeneratedRegex(@"(?:^|[\\/])plan-([a-z0-9]+(?:-[a-z0-9]+)*)\.md$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex PlanFilePathPattern();

    [GeneratedRegex(@"(?:^|[\\/])review-([a-z0-9]+(?:-[a-z0-9]+)*)\.md$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReviewFilePathPattern();
}

public sealed record ChatSummaryResponse(
    Guid Id,
    string Title,
    string Preview,
    DateTimeOffset UpdatedAt,
    string AgentId,
    Guid? ProjectId,
    Guid? WorkspaceId,
    string WorkspacePath,
    string Mode,
    string? ModelId,
    Guid? ParentChatId,
    string? PlanFilePath,
    ChatStatus Status,
    DateTimeOffset? LastReadAt);

public sealed record ChatDetailResponse(
    Guid Id,
    string Title,
    string AgentId,
    Guid? ProjectId,
    Guid? WorkspaceId,
    string WorkspacePath,
    string Mode,
    string? ModelId,
    Guid? ParentChatId,
    string? PlanFilePath,
    ChatStatus Status,
    DateTimeOffset? LastReadAt,
    IReadOnlyList<ChatMessageResponse> Messages);

public sealed record ChatMessageResponse(
    Guid Id,
    string Role,
    string Content,
    DateTimeOffset CreatedAt,
    string Status);

public sealed record CreateChatRequest(
    string Agent,
    Guid WorkspaceId,
    string? Mode = null,
    string? ModelId = null);

public sealed record UpdateChatModeRequest(string Mode);

public sealed record UpdateChatModeResponse(Guid Id, string Mode);

public sealed record UpdateChatModelRequest(string? ModelId);

public sealed record UpdateChatModelResponse(Guid Id, string? ModelId);

public sealed record SendMessageRequest(string Content);

public sealed record CreateChatResponse(
    Guid Id,
    string AgentId,
    Guid? ProjectId,
    Guid? WorkspaceId,
    string WorkspacePath,
    string Mode,
    string? ModelId,
    Guid? ParentChatId,
    string? PlanFilePath);

public sealed record KickOffPlanRequest(
    string PlanId,
    string Title,
    string ContentMarkdown);

public sealed record KickOffPlanResponse(
    Guid ChildChatId,
    string PlanFilePath,
    string InitialPrompt,
    string KickoffMessage);

public sealed record KickOffReviewResponse(
    Guid ReviewChildChatId,
    string ReviewFilePath,
    string InitialPrompt);

public sealed record SendMessageDoneResponse(Guid MessageId);

public sealed record SseStatusPayload(string Phase);

public sealed record SseTokenPayload(string Text);

public sealed record SseToolPayload(string Label);

public sealed record SseErrorPayload(string Code, string Message);

public sealed record ChatStatusSsePayload(Guid ChatId, ChatStatus Status);

public sealed record ChatStatusSnapshotItem(Guid ChatId, ChatStatus Status);
