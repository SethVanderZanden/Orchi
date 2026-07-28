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

    public static bool IsPdf(string fileName, string contentType) =>
        contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
        || Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    public static bool IsExcel(string fileName, string contentType)
    {
        string extension = Path.GetExtension(fileName);
        return contentType.Equals(
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".xls", StringComparison.OrdinalIgnoreCase);
    }

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
}
