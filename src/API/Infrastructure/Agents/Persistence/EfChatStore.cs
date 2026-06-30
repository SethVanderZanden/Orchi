using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Modes;
using DomainChatMessage = Orchi.Api.Infrastructure.Agents.ChatMessage;

namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed class EfChatStore(IDbContextFactory<AppDbContext> dbContextFactory) : IChatStore
{
    public async Task<ChatSession> CreateAsync(ChatCreateModel model, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var entity = new Chat
        {
            Id = model.Id,
            AgentId = model.AgentId,
            WorkspacePath = model.WorkspacePath,
            Mode = ChatStoreMapper.ModeToString(model.Mode),
            ParentChatId = model.ParentChatId,
            AttachedPlanId = model.AttachedPlanId,
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
            .Include(chat => chat.GoalJournal)
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
            .Include(chat => chat.GoalJournal)
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

    public async Task UpdateGoalChatIdAsync(Guid chatId, Guid goalChatId, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);
        if (chat is null)
        {
            return;
        }

        chat.GoalChatId = goalChatId;
        chat.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateModeAsync(
        Guid chatId,
        ChatMode mode,
        Guid? attachedPlanId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);
        if (chat is null)
        {
            return;
        }

        chat.Mode = ChatStoreMapper.ModeToString(mode);
        chat.AttachedPlanId = attachedPlanId;
        chat.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AppendGoalJournalAsync(Guid chatId, string entry, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Chat? chat = await db.Chats.FirstOrDefaultAsync(existing => existing.Id == chatId, cancellationToken);
        if (chat is null)
        {
            return;
        }

        db.GoalJournalEntries.Add(new GoalJournalEntry
        {
            Id = Guid.NewGuid(),
            ChatId = chatId,
            Content = entry,
            CreatedAt = DateTimeOffset.UtcNow
        });

        chat.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}
