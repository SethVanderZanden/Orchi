using Orchi.Api.Entities;
using DomainChatMessage = Orchi.Api.Infrastructure.Agents.ChatMessage;

namespace Orchi.Api.Infrastructure.Agents.Persistence;

internal static class ChatStoreMapper
{
    public static ChatSession ToSession(Chat entity)
    {
        var session = new ChatSession
        {
            Id = entity.Id,
            AgentId = entity.AgentId,
            ProjectId = entity.ProjectId,
            WorkspaceId = entity.WorkspaceId,
            WorkspacePath = entity.WorkspacePath,
            Mode = entity.Mode,
            ParentChatId = entity.ParentChatId,
            PlanFilePath = entity.PlanFilePath,
            ExternalSessionId = entity.ExternalSessionId
        };

        foreach (ChatMessageEntity message in entity.Messages.OrderBy(message => message.Ordinal))
        {
            session.Messages.Add(ToDomainMessage(message));
        }

        return session;
    }

    public static DomainChatMessage ToDomainMessage(ChatMessageEntity entity) =>
        new(entity.Id, entity.Role, entity.Content, entity.CreatedAt, entity.Status);
}
