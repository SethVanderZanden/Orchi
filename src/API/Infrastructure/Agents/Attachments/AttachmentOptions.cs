namespace Orchi.Api.Infrastructure.Agents.Attachments;

public sealed class AttachmentOptions
{
    public const string SectionName = "Attachments";

    /// <summary>When empty, uses %LocalApplicationData%/Orchi/attachment-blobs.</summary>
    public string? BlobRoot { get; set; }

    public long MaxFileSizeBytes { get; set; } = 26_214_400;

    public int MaxFilesPerMessage { get; set; } = 10;

    public int MaxExtractedTextChars { get; set; } = 50_000;
}
