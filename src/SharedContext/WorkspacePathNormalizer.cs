namespace Orchi.SharedContext;

public static class WorkspacePathNormalizer
{
    public static string Normalize(string workspacePath) =>
        Path.GetFullPath(workspacePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}
