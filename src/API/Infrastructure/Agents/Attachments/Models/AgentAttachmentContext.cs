namespace Orchi.Api.Infrastructure.Agents.Attachments.Models;

public sealed record AttachmentPromptItem(
    string WorkspaceRelativePath,
    string FileName,
    string ContentType,
    string? ExtractedTextPreview,
    bool IsImage);

public sealed record AgentAttachmentContext(
    IReadOnlyList<AttachmentPromptItem> PromptItems,
    IReadOnlyList<string> ImageAbsolutePaths)
{
    public static AgentAttachmentContext Empty { get; } =
        new([], []);
}
