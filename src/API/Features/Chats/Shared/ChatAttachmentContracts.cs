using Orchi.Api.Infrastructure.Agents.Attachments.Models;

namespace Orchi.Api.Features.Chats.Shared;

public static class ChatAttachmentMapper
{
    public static AttachmentResponse ToResponse(StoredAttachment attachment) =>
        new(
            attachment.Id,
            attachment.MessageId,
            attachment.FileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.CreatedAt);
}

public sealed record AttachmentResponse(
    Guid Id,
    Guid? MessageId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset CreatedAt);
