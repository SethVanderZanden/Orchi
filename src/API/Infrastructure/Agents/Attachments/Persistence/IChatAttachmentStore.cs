using Orchi.Api.Infrastructure.Agents.Attachments.Models;

namespace Orchi.Api.Infrastructure.Agents.Attachments.Persistence;

public sealed record CreateStagedAttachmentModel(
    Guid Id,
    Guid ChatId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string WorkspaceRelativePath,
    string? ExtractedText);

public interface IChatAttachmentStore
{
    Task<StoredAttachment> CreateStagedAsync(
        CreateStagedAttachmentModel model,
        CancellationToken cancellationToken);

    Task<bool> LinkToMessageAsync(
        Guid attachmentId,
        Guid chatId,
        Guid messageId,
        int ordinal,
        CancellationToken cancellationToken);

    Task<StoredAttachment?> GetAsync(Guid chatId, Guid attachmentId, CancellationToken cancellationToken);

    Task<IReadOnlyList<StoredAttachment>> ListByChatAsync(Guid chatId, CancellationToken cancellationToken);

    Task<IReadOnlyList<StoredAttachment>> ListByMessageAsync(
        Guid chatId,
        Guid messageId,
        CancellationToken cancellationToken);

    Task<bool> DeleteStagedAsync(Guid chatId, Guid attachmentId, CancellationToken cancellationToken);
}
