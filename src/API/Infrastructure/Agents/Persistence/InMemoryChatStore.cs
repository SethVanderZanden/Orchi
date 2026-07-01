using System.Collections.Concurrent;
using Orchi.Api.Infrastructure.Agents.Modes;

namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed class InMemoryChatStore : IChatStore
{
    private readonly ConcurrentDictionary<Guid, ChatSession> _chats = new();

    public Task<ChatSession> CreateAsync(ChatCreateModel model, CancellationToken cancellationToken)
    {
        var session = new ChatSession
        {
            Id = model.Id,
            AgentId = model.AgentId,
            WorkspacePath = model.WorkspacePath,
            Mode = model.Mode,
            ParentChatId = model.ParentChatId,
            AttachedPlanId = model.AttachedPlanId
        };

        _chats[session.Id] = session;
        return Task.FromResult(session);
    }

    public Task<ChatSession?> GetAsync(Guid chatId, CancellationToken cancellationToken) =>
        Task.FromResult(_chats.TryGetValue(chatId, out ChatSession? session) ? session : null);

    public Task<bool> ExistsAsync(Guid chatId, CancellationToken cancellationToken) =>
        Task.FromResult(_chats.ContainsKey(chatId));

    public Task<IReadOnlyList<ChatSession>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ChatSession>>(_chats.Values
            .OrderByDescending(session => session.Messages.LastOrDefault()?.CreatedAt ?? DateTimeOffset.MinValue)
            .ToArray());

    public Task<bool> DeleteAsync(Guid chatId, CancellationToken cancellationToken) =>
        Task.FromResult(_chats.TryRemove(chatId, out _));

    public Task SaveUserMessageAsync(Guid chatId, ChatMessage message, CancellationToken cancellationToken)
    {
        if (_chats.TryGetValue(chatId, out ChatSession? session))
        {
            lock (session.Sync)
            {
                if (session.Messages.All(existing => existing.Id != message.Id))
                {
                    session.Messages.Add(message);
                }
            }
        }

        return Task.CompletedTask;
    }

    public Task SaveAssistantMessageAsync(
        Guid chatId,
        ChatMessage message,
        string? externalSessionId,
        CancellationToken cancellationToken)
    {
        if (!_chats.TryGetValue(chatId, out ChatSession? session))
        {
            return Task.CompletedTask;
        }

        lock (session.Sync)
        {
            int index = session.Messages.FindIndex(existing => existing.Id == message.Id);
            if (index >= 0)
            {
                session.Messages[index] = message;
            }
            else
            {
                session.Messages.Add(message);
            }

            if (!string.IsNullOrWhiteSpace(externalSessionId))
            {
                session.ExternalSessionId = externalSessionId;
            }
        }

        return Task.CompletedTask;
    }

    public Task UpdateExternalSessionIdAsync(
        Guid chatId,
        string externalSessionId,
        CancellationToken cancellationToken)
    {
        if (_chats.TryGetValue(chatId, out ChatSession? session))
        {
            session.ExternalSessionId = externalSessionId;
        }

        return Task.CompletedTask;
    }

    public Task UpdateGoalChatIdAsync(Guid chatId, Guid goalChatId, CancellationToken cancellationToken)
    {
        if (_chats.TryGetValue(chatId, out ChatSession? session))
        {
            session.GoalChatId = goalChatId;
        }

        return Task.CompletedTask;
    }

    public Task UpdateModeAsync(
        Guid chatId,
        ChatMode mode,
        Guid? attachedPlanId,
        CancellationToken cancellationToken)
    {
        if (_chats.TryGetValue(chatId, out ChatSession? session))
        {
            session.Mode = mode;
            session.AttachedPlanId = attachedPlanId;
            session.ExternalSessionId = null;
        }

        return Task.CompletedTask;
    }

    public Task AppendGoalJournalAsync(Guid chatId, string entry, CancellationToken cancellationToken)
    {
        if (_chats.TryGetValue(chatId, out ChatSession? session))
        {
            lock (session.Sync)
            {
                session.GoalJournal.Add(entry);
            }
        }

        return Task.CompletedTask;
    }
}
