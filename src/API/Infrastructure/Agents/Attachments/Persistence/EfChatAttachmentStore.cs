using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Attachments.Models;

namespace Orchi.Api.Infrastructure.Agents.Attachments.Persistence;

public sealed class EfChatAttachmentStore(IDbContextFactory<AppDbContext> dbContextFactory) : IChatAttachmentStore
{
    public async Task<StoredAttachment> CreateStagedAsync(
        CreateStagedAttachmentModel model,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        var entity = new ChatMessageAttachment
        {
            Id = model.Id,
            ChatId = model.ChatId,
            MessageId = null,
            FileName = model.FileName,
            ContentType = model.ContentType,
            SizeBytes = model.SizeBytes,
            WorkspaceRelativePath = model.WorkspaceRelativePath,
            ExtractedText = model.ExtractedText,
            CreatedAt = now,
            Ordinal = 0
        };

        db.ChatMessageAttachments.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return ToStored(entity);
    }

    public async Task<bool> LinkToMessageAsync(
        Guid attachmentId,
        Guid chatId,
        Guid messageId,
        int ordinal,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ChatMessageAttachment? entity = await db.ChatMessageAttachments
            .FirstOrDefaultAsync(
                attachment => attachment.Id == attachmentId
                    && attachment.ChatId == chatId
                    && attachment.MessageId == null,
                cancellationToken);

        if (entity is null)
        {
            return false;
        }

        entity.MessageId = messageId;
        entity.Ordinal = ordinal;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<StoredAttachment?> GetAsync(
        Guid chatId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ChatMessageAttachment? entity = await db.ChatMessageAttachments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                attachment => attachment.Id == attachmentId && attachment.ChatId == chatId,
                cancellationToken);

        return entity is null ? null : ToStored(entity);
    }

    public async Task<IReadOnlyList<StoredAttachment>> ListByChatAsync(
        Guid chatId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<ChatMessageAttachment> entities = await db.ChatMessageAttachments
            .AsNoTracking()
            .Where(attachment => attachment.ChatId == chatId)
            .OrderBy(attachment => attachment.CreatedAt)
            .ThenBy(attachment => attachment.Ordinal)
            .ToListAsync(cancellationToken);

        return entities.Select(ToStored).ToArray();
    }

    public async Task<IReadOnlyList<StoredAttachment>> ListByMessageAsync(
        Guid chatId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        List<ChatMessageAttachment> entities = await db.ChatMessageAttachments
            .AsNoTracking()
            .Where(attachment => attachment.ChatId == chatId && attachment.MessageId == messageId)
            .OrderBy(attachment => attachment.Ordinal)
            .ToListAsync(cancellationToken);

        return entities.Select(ToStored).ToArray();
    }

    public async Task<bool> DeleteStagedAsync(
        Guid chatId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        ChatMessageAttachment? entity = await db.ChatMessageAttachments
            .FirstOrDefaultAsync(
                attachment => attachment.Id == attachmentId
                    && attachment.ChatId == chatId
                    && attachment.MessageId == null,
                cancellationToken);

        if (entity is null)
        {
            return false;
        }

        db.ChatMessageAttachments.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static StoredAttachment ToStored(ChatMessageAttachment entity) =>
        new(
            entity.Id,
            entity.ChatId,
            entity.MessageId,
            entity.FileName,
            entity.ContentType,
            entity.SizeBytes,
            entity.WorkspaceRelativePath,
            entity.ExtractedText,
            entity.CreatedAt,
            entity.Ordinal);
}
