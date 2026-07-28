using Orchi.Api.Entities;
using DomainChatMessage = Orchi.Api.Infrastructure.Agents.ChatMessage;

namespace Orchi.Api.Infrastructure.Agents.Persistence;

internal static class ChatStoreMapper
{
    public static ChatSession ToSession(Chat entity)
    {
        ChatSession session = ToSessionShell(entity);

        foreach (ChatMessageEntity message in entity.Messages.OrderBy(message => message.Ordinal))
        {
            session.Messages.Add(ToDomainMessage(message));
        }

        return session;
    }

    /// <summary>
    /// Builds a list/summary session with only the messages needed for title and preview derivation.
    /// </summary>
    public static ChatSession ToSessionSummary(
        Chat entity,
        IReadOnlyList<ChatMessageEntity> summaryMessages)
    {
        ChatSession session = ToSessionShell(entity);

        foreach (ChatMessageEntity message in summaryMessages.OrderBy(message => message.Ordinal))
        {
            session.Messages.Add(ToDomainMessage(message));
        }

        return session;
    }

    private static ChatSession ToSessionShell(Chat entity) =>
        new()
        {
            Id = entity.Id,
            AgentId = entity.AgentId,
            ProjectId = entity.ProjectId,
            WorkspaceId = entity.WorkspaceId,
            WorkspacePath = entity.WorkspacePath,
            Mode = entity.Mode,
            ModelId = entity.ModelId,
            ContextSizeId = entity.ContextSizeId,
            ReasoningEffortId = entity.ReasoningEffortId,
            ApprovalPolicyId = entity.ApprovalPolicyId,
            ParentChatId = entity.ParentChatId,
            PlanFilePath = entity.PlanFilePath,
            ExternalSessionId = entity.ExternalSessionId,
            Status = entity.Status,
            LastReadAt = entity.LastReadAt
        };

    public static DomainChatMessage ToDomainMessage(ChatMessageEntity entity) =>
        new(entity.Id, entity.Role, entity.Content, entity.CreatedAt, entity.Status);
}
