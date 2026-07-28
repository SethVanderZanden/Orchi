using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Search;
using DomainChatMessage = Orchi.Api.Infrastructure.Agents.ChatMessage;

namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed class EfChatStore(
    IDbContextFactory<AppDbContext> dbContextFactory,
    ChatSearchComposer searchComposer) : IChatStore
{
    public async Task<ChatSession> CreateAsync(ChatCreateModel model, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var entity = new Chat
        {
            Id = model.Id,
            AgentId = model.AgentId,
            ProjectId = model.ProjectId,
            WorkspaceId = model.WorkspaceId,
            WorkspacePath = model.WorkspacePath,
            Mode = model.Mode,
            ParentChatId = model.ParentChatId,
            PlanFilePath = model.PlanFilePath,
            ModelId = model.ModelId,
            ContextSizeId = model.ContextSizeId,
            ReasoningEffortId = model.ReasoningEffortId,
            ApprovalPolicyId = model.ApprovalPolicyId,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Chats.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ChatStoreMapper.ToSession(entity);
    }

    public async Task<ChatSession?> GetAsync(Guid chatId, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? entity = await db.Chats
            .AsNoTracking()
            .Include(chat => chat.Messages)
            .FirstOrDefaultAsync(chat => chat.Id == chatId, cancellationToken);

        return entity is null ? null : ChatStoreMapper.ToSession(entity);
    }

    public async Task<bool> ExistsAsync(Guid chatId, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Chats.AnyAsync(chat => chat.Id == chatId, cancellationToken);
    }

    public async Task<IReadOnlyList<ChatSession>> ListAsync(CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        List<Chat> entities = await db.Chats
            .AsNoTracking()
            .Include(chat => chat.Messages)
            .ToListAsync(cancellationToken);

        return entities
            .OrderByDescending(chat => chat.UpdatedAt)
            .Select(ChatStoreMapper.ToSession)
            .ToArray();
    }

    public async Task<IReadOnlyList<ChatSession>> SearchAsync(
        ChatSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Chat> filtered = searchComposer.Apply(db.Chats.AsNoTracking(), criteria);
        int limit = criteria.ResolveLimit();

        // SQLite cannot ORDER BY DateTimeOffset in SQL — project then order in memory (same as ListAsync).
        var projected = await filtered
            .Select(chat => new { chat.Id, chat.UpdatedAt })
            .ToListAsync(cancellationToken);

        List<Guid> orderedIds = projected
            .OrderByDescending(chat => chat.UpdatedAt)
            .Take(limit)
            .Select(chat => chat.Id)
            .ToList();

        if (orderedIds.Count == 0)
        {
            return [];
        }

        List<Chat> entities = await db.Chats
            .AsNoTracking()
            .Include(chat => chat.Messages)
            .Where(chat => orderedIds.Contains(chat.Id))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, Chat> byId = entities.ToDictionary(chat => chat.Id);
        return orderedIds
            .Where(byId.ContainsKey)
            .Select(id => ChatStoreMapper.ToSession(byId[id]))
            .ToArray();
    }

    public async Task<IReadOnlyList<ChatSession>> ListChildrenAsync(
        Guid parentChatId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        List<Chat> entities = await db.Chats
            .AsNoTracking()
            .Where(chat => chat.ParentChatId == parentChatId && !chat.IsDeleted)
            .ToListAsync(cancellationToken);

        return entities
            .OrderByDescending(chat => chat.UpdatedAt)
            .Select(ChatStoreMapper.ToSession)
            .ToArray();
    }

    public async Task<bool> DeleteAsync(Guid chatId, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? entity = await db.Chats.FirstOrDefaultAsync(chat => chat.Id == chatId, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task SaveUserMessageAsync(Guid chatId, DomainChatMessage message, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);
        if (chat is null)
        {
            return;
        }

        int ordinal = await db.ChatMessages.CountAsync(existing => existing.ChatId == chatId, cancellationToken);
        db.ChatMessages.Add(new ChatMessageEntity
        {
            Id = message.Id,
            ChatId = chatId,
            Role = message.Role,
            Content = message.Content,
            Status = message.Status,
            CreatedAt = message.CreatedAt,
            Ordinal = ordinal
        });

        chat.UpdatedAt = message.CreatedAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAssistantMessageAsync(
        Guid chatId,
        DomainChatMessage message,
        string? externalSessionId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);
        if (chat is null)
        {
            return;
        }

        ChatMessageEntity? existing = await db.ChatMessages
            .FirstOrDefaultAsync(row => row.Id == message.Id, cancellationToken);

        if (existing is null)
        {
            int ordinal = await db.ChatMessages.CountAsync(row => row.ChatId == chatId, cancellationToken);
            db.ChatMessages.Add(new ChatMessageEntity
            {
                Id = message.Id,
                ChatId = chatId,
                Role = message.Role,
                Content = message.Content,
                Status = message.Status,
                CreatedAt = message.CreatedAt,
                Ordinal = ordinal
            });
        }
        else
        {
            existing.Content = message.Content;
            existing.Status = message.Status;
        }

        if (!string.IsNullOrWhiteSpace(externalSessionId))
        {
            chat.ExternalSessionId = externalSessionId;
        }

        chat.UpdatedAt = message.CreatedAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveStatusMessageAsync(
        Guid chatId,
        DomainChatMessage message,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);
        if (chat is null)
        {
            return;
        }

        int ordinal = await db.ChatMessages.CountAsync(existing => existing.ChatId == chatId, cancellationToken);
        db.ChatMessages.Add(new ChatMessageEntity
        {
            Id = message.Id,
            ChatId = chatId,
            Role = message.Role,
            Content = message.Content,
            Status = message.Status,
            CreatedAt = message.CreatedAt,
            Ordinal = ordinal
        });

        chat.UpdatedAt = message.CreatedAt;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateExternalSessionIdAsync(
        Guid chatId,
        string externalSessionId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);
        if (chat is null)
        {
            return;
        }

        chat.ExternalSessionId = externalSessionId;
        chat.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateModeAsync(Guid chatId, string mode, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);

        if (chat is null)
        {
            return false;
        }

        chat.Mode = mode;
        chat.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateModelIdAsync(Guid chatId, string? modelId, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);

        if (chat is null)
        {
            return false;
        }

        chat.ModelId = string.IsNullOrWhiteSpace(modelId) ? null : modelId.Trim();
        chat.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateContextSizeIdAsync(
        Guid chatId,
        string? contextSizeId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);

        if (chat is null)
        {
            return false;
        }

        chat.ContextSizeId = string.IsNullOrWhiteSpace(contextSizeId) ? null : contextSizeId.Trim();
        chat.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateReasoningEffortIdAsync(
        Guid chatId,
        string? reasoningEffortId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);

        if (chat is null)
        {
            return false;
        }

        chat.ReasoningEffortId = string.IsNullOrWhiteSpace(reasoningEffortId) ? null : reasoningEffortId.Trim();
        chat.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateApprovalPolicyIdAsync(
        Guid chatId,
        string? approvalPolicyId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);

        if (chat is null)
        {
            return false;
        }

        chat.ApprovalPolicyId = string.IsNullOrWhiteSpace(approvalPolicyId) ? null : approvalPolicyId.Trim();
        chat.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateWorkspaceAsync(
        Guid chatId,
        Guid projectId,
        Guid workspaceId,
        string workspacePath,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);

        if (chat is null)
        {
            return false;
        }

        chat.ProjectId = projectId;
        chat.WorkspaceId = workspaceId;
        chat.WorkspacePath = workspacePath;
        chat.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateRuntimeAsync(
        Guid chatId,
        string agentId,
        string mode,
        string? modelId,
        string? contextSizeId,
        string? reasoningEffortId,
        string? approvalPolicyId,
        bool clearExternalSessionId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);

        if (chat is null)
        {
            return false;
        }

        chat.AgentId = agentId;
        chat.Mode = mode;
        chat.ModelId = string.IsNullOrWhiteSpace(modelId) ? null : modelId.Trim();
        chat.ContextSizeId = string.IsNullOrWhiteSpace(contextSizeId) ? null : contextSizeId.Trim();
        chat.ReasoningEffortId = string.IsNullOrWhiteSpace(reasoningEffortId) ? null : reasoningEffortId.Trim();
        chat.ApprovalPolicyId = string.IsNullOrWhiteSpace(approvalPolicyId) ? null : approvalPolicyId.Trim();

        if (clearExternalSessionId)
        {
            chat.ExternalSessionId = null;
        }

        chat.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ChatStatus?> UpdateStatusAsync(
        Guid chatId,
        ChatStatus status,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);
        if (chat is null)
        {
            return null;
        }

        if (chat.Status == status)
        {
            return status;
        }

        chat.Status = status;
        chat.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return status;
    }

    public async Task<ChatSession?> MarkReadAsync(
        Guid chatId,
        bool clearInProgress,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Atomic update avoids a tracked-entity SaveChanges race with concurrent status writes.
        int affected = clearInProgress
            ? await db.Chats
                .Where(chat => chat.Id == chatId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(chat => chat.LastReadAt, now)
                        .SetProperty(chat => chat.UpdatedAt, now)
                        .SetProperty(chat => chat.Status, ChatStatus.Read),
                    cancellationToken)
            : await db.Chats
                .Where(chat => chat.Id == chatId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(chat => chat.LastReadAt, now)
                        .SetProperty(chat => chat.UpdatedAt, now)
                        .SetProperty(
                            chat => chat.Status,
                            chat => chat.Status == ChatStatus.InProgress
                                ? ChatStatus.InProgress
                                : ChatStatus.Read),
                    cancellationToken);

        if (affected == 0)
        {
            return null;
        }

        Chat? chat = await db.Chats
            .AsNoTracking()
            .Include(existing => existing.Messages)
            .FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);

        return chat is null ? null : ChatStoreMapper.ToSession(chat);
    }
}
