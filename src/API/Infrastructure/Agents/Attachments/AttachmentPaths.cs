namespace Orchi.Api.Infrastructure.Agents.Attachments;

internal static class AttachmentPaths
{
    public static string WorkspaceRelative(Guid attachmentId, string fileName) =>
        $".orchi/attachments/{attachmentId:D}/{SanitizeFileName(fileName)}";

    public static string Absolute(string workspacePath, string workspaceRelativePath) =>
        Path.Combine(
            workspacePath,
            workspaceRelativePath.Replace('/', Path.DirectorySeparatorChar));

    public static string StagedBlobPath(string blobRoot, Guid chatId, Guid attachmentId) =>
        Path.Combine(blobRoot, "staged", chatId.ToString("D"), $"{attachmentId:D}.bin");

    public static bool IsImageContentType(string contentType) =>
        contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    public static string SanitizeFileName(string fileName)
    {
        string name = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(name))
        {
            return "attachment";
        }

        foreach (char invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '_');
        }

        return name;
    }

    /// <summary>
    /// Prefer a real MIME when the client sends empty / octet-stream but the extension is known
    /// (common for PDF and Excel uploads from Electron / OS pickers).
    /// </summary>
    public static string NormalizeContentType(string fileName, string? contentType)
    {
        string trimmed = string.IsNullOrWhiteSpace(contentType)
            ? string.Empty
            : contentType.Trim();

        bool isGeneric = string.IsNullOrEmpty(trimmed)
            || trimmed.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase);

        if (!isGeneric)
        {
            return trimmed;
        }

        return InferContentTypeFromExtension(Path.GetExtension(fileName)) ?? "application/octet-stream";
    }

    public static string? InferContentTypeFromExtension(string? extension)
    {
        return extension?.ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".xlsm" => "application/vnd.ms-excel.sheet.macroEnabled.12",
            ".xls" => "application/vnd.ms-excel",
            ".csv" => "text/csv",
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".yaml" or ".yml" => "application/yaml",
            ".log" => "text/plain",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => null
        };
    }

    public static AttachmentKind ResolveKind(string fileName, string contentType)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (IsImageContentType(contentType) || extension is ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp")
        {
            return AttachmentKind.Image;
        }

        if (contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            || extension is ".pdf")
        {
            return AttachmentKind.Pdf;
        }

        if (IsSpreadsheet(contentType, extension))
        {
            return AttachmentKind.Spreadsheet;
        }

        if (contentType.Equals("text/csv", StringComparison.OrdinalIgnoreCase)
            || extension is ".csv")
        {
            return AttachmentKind.Csv;
        }

        if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || extension is ".txt" or ".md" or ".json" or ".xml" or ".yaml" or ".yml" or ".log")
        {
            return AttachmentKind.Text;
        }

        return AttachmentKind.Other;
    }

    private static bool IsSpreadsheet(string contentType, string extension)
    {
        if (extension is ".xlsx" or ".xls" or ".xlsm")
        {
            return true;
        }

        return contentType.Contains("spreadsheet", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals(
                "application/vnd.ms-excel.sheet.macroEnabled.12",
                StringComparison.OrdinalIgnoreCase);
    }
}
