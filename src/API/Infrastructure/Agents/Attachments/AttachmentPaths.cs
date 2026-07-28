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
}
