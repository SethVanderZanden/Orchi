namespace Orchi.Api.Common;

/// <summary>
/// Path normalization aligned with desktop <c>normalizeWorkspacePath</c> in
/// <c>src/desktop/src/renderer/src/lib/workspaces/store.ts</c>.
/// </summary>
public static class WorkspacePathNormalizer
{
    public static string Normalize(string path)
    {
        string trimmed = path.Trim().TrimEnd('/', '\\');
        string withBackslashes = trimmed.Replace('/', '\\');

        if (withBackslashes.Length >= 3
            && char.IsAsciiLetter(withBackslashes[0])
            && withBackslashes[1] == ':'
            && withBackslashes[2] == '\\')
        {
            return withBackslashes.ToLowerInvariant();
        }

        return trimmed.Replace('\\', '/');
    }

    public static string DeriveNameFromPath(string path)
    {
        string trimmed = path.Trim().TrimEnd('/', '\\');
        string segment = trimmed.Replace('\\', '/');
        int lastSlash = segment.LastIndexOf('/');
        return lastSlash >= 0 ? segment[(lastSlash + 1)..] : segment;
    }
}
