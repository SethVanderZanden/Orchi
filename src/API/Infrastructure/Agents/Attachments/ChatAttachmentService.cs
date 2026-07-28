using System.Text;
using Microsoft.Extensions.Options;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Attachments.Models;
using Orchi.Api.Infrastructure.Agents.Attachments.Persistence;

namespace Orchi.Api.Infrastructure.Agents.Attachments;

public sealed class ChatAttachmentService(
    IChatAttachmentStore store,
    IOptions<AttachmentOptions> options,
    ILogger<ChatAttachmentService> logger)
{
    private readonly AttachmentOptions _options = options.Value;

    public async Task<Result<StoredAttachment>> StageUploadAsync(
        Guid chatId,
        string fileName,
        string contentType,
        long sizeBytes,
        Stream content,
        CancellationToken cancellationToken)
    {
        if (sizeBytes > _options.MaxFileSizeBytes)
        {
            return Result.Failure<StoredAttachment>(
                Error.Validation(
                    "Attachment.TooLarge",
                    $"File exceeds the {_options.MaxFileSizeBytes / (1024 * 1024)} MB limit."));
        }

        Guid attachmentId = Guid.NewGuid();
        string sanitizedFileName = AttachmentPaths.SanitizeFileName(fileName);
        string relativePath = AttachmentPaths.WorkspaceRelative(attachmentId, sanitizedFileName);
        string normalizedContentType = string.IsNullOrWhiteSpace(contentType)
            ? "application/octet-stream"
            : contentType.Trim();

        string blobPath = AttachmentPaths.StagedBlobPath(GetBlobRoot(), chatId, attachmentId);
        Directory.CreateDirectory(Path.GetDirectoryName(blobPath)!);

        long writtenBytes;
        try
        {
            writtenBytes = await CopyToBlobWithLimitAsync(content, blobPath, cancellationToken);
        }
        catch (AttachmentTooLargeException)
        {
            TryDeleteBlob(blobPath);
            return Result.Failure<StoredAttachment>(
                Error.Validation(
                    "Attachment.TooLarge",
                    $"File exceeds the {_options.MaxFileSizeBytes / (1024 * 1024)} MB limit."));
        }

        if (writtenBytes == 0)
        {
            TryDeleteBlob(blobPath);
            return Result.Failure<StoredAttachment>(
                Error.Validation("Attachment.Empty", "File is empty."));
        }

        string? extractedText = await TryExtractTextAsync(
            sanitizedFileName,
            normalizedContentType,
            blobPath,
            cancellationToken);

        StoredAttachment stored = await store.CreateStagedAsync(
            new CreateStagedAttachmentModel(
                attachmentId,
                chatId,
                sanitizedFileName,
                normalizedContentType,
                writtenBytes,
                relativePath,
                extractedText),
            cancellationToken);

        return Result.Success(stored);
    }

    private async Task<long> CopyToBlobWithLimitAsync(
        Stream content,
        string blobPath,
        CancellationToken cancellationToken)
    {
        await using FileStream blobStream = File.Create(blobPath);
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            total += read;
            if (total > _options.MaxFileSizeBytes)
            {
                throw new AttachmentTooLargeException();
            }

            await blobStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return total;
    }

    private sealed class AttachmentTooLargeException : Exception;

    public async Task<Result> DeleteStagedAsync(Guid chatId, Guid attachmentId, CancellationToken cancellationToken)
    {
        bool deleted = await store.DeleteStagedAsync(chatId, attachmentId, cancellationToken);
        if (!deleted)
        {
            return Result.Failure(Error.NotFound($"Staged attachment '{attachmentId}' was not found."));
        }

        TryDeleteBlob(AttachmentPaths.StagedBlobPath(GetBlobRoot(), chatId, attachmentId));
        return Result.Success();
    }

    public Task<IReadOnlyList<StoredAttachment>> ListByChatAsync(Guid chatId, CancellationToken cancellationToken) =>
        store.ListByChatAsync(chatId, cancellationToken);

    public async Task<Result<AgentAttachmentContext>> PrepareTurnAsync(
        Guid chatId,
        Guid messageId,
        string workspacePath,
        IReadOnlyList<Guid> attachmentIds,
        CancellationToken cancellationToken)
    {
        if (attachmentIds.Count == 0)
        {
            return Result.Success(AgentAttachmentContext.Empty);
        }

        if (attachmentIds.Count > _options.MaxFilesPerMessage)
        {
            return Result.Failure<AgentAttachmentContext>(
                Error.Validation(
                    "Attachment.TooMany",
                    $"A message can include at most {_options.MaxFilesPerMessage} attachments."));
        }

        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            return Result.Failure<AgentAttachmentContext>(
                Error.Validation("Workspace.Required", "A valid workspace is required to send attachments."));
        }

        var resolved = new List<StoredAttachment>();
        for (int ordinal = 0; ordinal < attachmentIds.Count; ordinal++)
        {
            Guid attachmentId = attachmentIds[ordinal];
            StoredAttachment? attachment = await store.GetAsync(chatId, attachmentId, cancellationToken);
            if (attachment is null || attachment.MessageId is not null)
            {
                return Result.Failure<AgentAttachmentContext>(
                    Error.Validation("Attachment.Invalid", $"Attachment '{attachmentId}' is not available."));
            }

            bool linked = await store.LinkToMessageAsync(
                attachmentId,
                chatId,
                messageId,
                ordinal,
                cancellationToken);

            if (!linked)
            {
                return Result.Failure<AgentAttachmentContext>(
                    Error.Validation("Attachment.Invalid", $"Attachment '{attachmentId}' could not be linked."));
            }

            StoredAttachment linkedAttachment = attachment with { MessageId = messageId, Ordinal = ordinal };
            await MirrorToWorkspaceAsync(linkedAttachment, workspacePath, cancellationToken);
            resolved.Add(linkedAttachment);
        }

        return Result.Success(BuildAgentContext(workspacePath, resolved));
    }

    public async Task OnWorkspaceChangedAsync(
        Guid chatId,
        string workspacePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            return;
        }

        IReadOnlyList<StoredAttachment> attachments = await store.ListByChatAsync(chatId, cancellationToken);
        foreach (StoredAttachment attachment in attachments)
        {
            try
            {
                await MirrorToWorkspaceAsync(attachment, workspacePath, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to rematerialize attachment {AttachmentId} for chat {ChatId} in workspace {WorkspacePath}",
                    attachment.Id,
                    chatId,
                    workspacePath);
            }
        }
    }

    public async Task<Result<(Stream Stream, string ContentType, string FileName)>> OpenReadAsync(
        Guid chatId,
        Guid attachmentId,
        string workspacePath,
        CancellationToken cancellationToken)
    {
        StoredAttachment? attachment = await store.GetAsync(chatId, attachmentId, cancellationToken);
        if (attachment is null)
        {
            return Result.Failure<(Stream, string, string)>(
                Error.NotFound($"Attachment '{attachmentId}' was not found."));
        }

        if (!string.IsNullOrWhiteSpace(workspacePath) && Directory.Exists(workspacePath))
        {
            await MirrorToWorkspaceAsync(attachment, workspacePath, cancellationToken);
            string absolutePath = AttachmentPaths.Absolute(workspacePath, attachment.WorkspaceRelativePath);
            if (File.Exists(absolutePath))
            {
                Stream workspaceStream = File.OpenRead(absolutePath);
                return Result.Success<(Stream, string, string)>(
                    (workspaceStream, attachment.ContentType, attachment.FileName));
            }
        }

        string stagedBlob = AttachmentPaths.StagedBlobPath(GetBlobRoot(), chatId, attachmentId);
        if (File.Exists(stagedBlob))
        {
            Stream blobStream = File.OpenRead(stagedBlob);
            return Result.Success<(Stream, string, string)>(
                (blobStream, attachment.ContentType, attachment.FileName));
        }

        return Result.Failure<(Stream, string, string)>(
            Error.NotFound($"Attachment content for '{attachmentId}' was not found."));
    }

    private async Task MirrorToWorkspaceAsync(
        StoredAttachment attachment,
        string workspacePath,
        CancellationToken cancellationToken)
    {
        string absolutePath = AttachmentPaths.Absolute(workspacePath, attachment.WorkspaceRelativePath);
        if (File.Exists(absolutePath))
        {
            return;
        }

        string? sourcePath = ResolveBlobPath(attachment);
        if (sourcePath is null || !File.Exists(sourcePath))
        {
            throw new FileNotFoundException(
                $"Attachment blob for '{attachment.Id}' was not found.",
                sourcePath);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);
        await using FileStream source = File.OpenRead(sourcePath);
        await using FileStream destination = File.Create(absolutePath);
        await source.CopyToAsync(destination, cancellationToken);
    }

    private string? ResolveBlobPath(StoredAttachment attachment)
    {
        string staged = AttachmentPaths.StagedBlobPath(GetBlobRoot(), attachment.ChatId, attachment.Id);
        if (File.Exists(staged))
        {
            return staged;
        }

        return null;
    }

    private AgentAttachmentContext BuildAgentContext(
        string workspacePath,
        IReadOnlyList<StoredAttachment> attachments)
    {
        var promptItems = new List<AttachmentPromptItem>();
        var imagePaths = new List<string>();

        foreach (StoredAttachment attachment in attachments)
        {
            bool isImage = AttachmentPaths.IsImageContentType(attachment.ContentType);
            promptItems.Add(new AttachmentPromptItem(
                attachment.WorkspaceRelativePath,
                attachment.FileName,
                attachment.ContentType,
                attachment.ExtractedText,
                isImage));

            if (isImage)
            {
                imagePaths.Add(AttachmentPaths.Absolute(workspacePath, attachment.WorkspaceRelativePath));
            }
        }

        return new AgentAttachmentContext(promptItems, imagePaths);
    }

    private async Task<string?> TryExtractTextAsync(
        string fileName,
        string contentType,
        string blobPath,
        CancellationToken cancellationToken)
    {
        if (AttachmentPaths.IsImageContentType(contentType))
        {
            return null;
        }

        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        bool isTextLike = contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || extension is ".csv" or ".txt" or ".md" or ".json" or ".xml" or ".yaml" or ".yml" or ".log";

        if (!isTextLike)
        {
            return null;
        }

        await using FileStream stream = File.OpenRead(blobPath);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        (string? text, bool truncated) = await ReadBoundedTextAsync(
            reader,
            _options.MaxExtractedTextChars,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        string trimmed = text.Trim();
        return truncated ? trimmed + "\n…(truncated)" : trimmed;
    }

    private static async Task<(string? Text, bool Truncated)> ReadBoundedTextAsync(
        StreamReader reader,
        int maxChars,
        CancellationToken cancellationToken)
    {
        if (maxChars <= 0)
        {
            return (null, false);
        }

        var builder = new StringBuilder(Math.Min(maxChars, 4096));
        var buffer = new char[4096];
        while (builder.Length < maxChars)
        {
            int toRead = Math.Min(buffer.Length, maxChars - builder.Length);
            int read = await reader.ReadAsync(buffer.AsMemory(0, toRead), cancellationToken);
            if (read <= 0)
            {
                break;
            }

            builder.Append(buffer, 0, read);
        }

        if (builder.Length == 0)
        {
            return (null, false);
        }

        if (builder.Length < maxChars)
        {
            return (builder.ToString(), false);
        }

        int peek = await reader.ReadAsync(buffer.AsMemory(0, 1), cancellationToken);
        return (builder.ToString(), peek > 0);
    }

    private string GetBlobRoot()
    {
        if (!string.IsNullOrWhiteSpace(_options.BlobRoot))
        {
            return _options.BlobRoot;
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Orchi",
            "attachment-blobs");
    }

    private void TryDeleteBlob(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete attachment blob at {Path}", path);
        }
    }
}
