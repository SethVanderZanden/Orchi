namespace Orchi.Api.Infrastructure.Agents.Attachments;

/// <summary>
/// Derived UI/agent classification for an attachment. Not persisted — resolved from
/// file name extension and content type. Bytes stay on disk; SQLite only stores metadata.
/// </summary>
public enum AttachmentKind
{
    Image,
    Pdf,
    Spreadsheet,
    Csv,
    Text,
    Other
}
