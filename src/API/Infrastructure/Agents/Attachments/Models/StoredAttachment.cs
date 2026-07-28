namespace Orchi.Api.Infrastructure.Agents.Attachments.Models;

public sealed record StoredAttachment(
    Guid Id,
    Guid ChatId,
    Guid? MessageId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string WorkspaceRelativePath,
    string? ExtractedText,
    DateTimeOffset CreatedAt,
    int Ordinal);
