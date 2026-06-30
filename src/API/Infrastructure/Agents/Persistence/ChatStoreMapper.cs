using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Modes;
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
            WorkspacePath = entity.WorkspacePath,
            Mode = ChatModeParser.TryParse(entity.Mode, out ChatMode mode) ? mode : ChatMode.Plan,
            ParentChatId = entity.ParentChatId,
            AttachedPlanId = entity.AttachedPlanId,
            GoalChatId = entity.GoalChatId,
            ExternalSessionId = entity.ExternalSessionId
        };

        foreach (ChatMessageEntity message in entity.Messages.OrderBy(message => message.Ordinal))
        {
            session.Messages.Add(ToDomainMessage(message));
        }

        foreach (GoalJournalEntry entry in entity.GoalJournal.OrderBy(entry => entry.CreatedAt))
        {
            session.GoalJournal.Add(entry.Content);
        }

        return session;
    }

    public static DomainChatMessage ToDomainMessage(ChatMessageEntity entity) =>
        new(entity.Id, entity.Role, entity.Content, entity.CreatedAt, entity.Status);

    public static string ModeToString(ChatMode mode) => ChatModeParser.ToApiString(mode);
}
