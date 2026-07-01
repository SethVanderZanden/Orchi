namespace Orchi.SharedContext.Indexing;

internal static class WorkspaceFileDiscovery
{
    private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".orchi",
        "node_modules",
        "bin",
        "obj",
        "dist",
        "build",
        ".next",
        "coverage"
    };

    private static readonly HashSet<string> IncludedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".ts", ".tsx", ".js", ".jsx", ".md", ".json", ".csproj", ".sln", ".css", ".html", ".yaml", ".yml"
    };

    public static IEnumerable<string> EnumerateSourceFiles(string workspacePath, int maxFiles)
    {
        if (!Directory.Exists(workspacePath))
        {
            yield break;
        }

        int count = 0;
        var stack = new Stack<string>();
        stack.Push(workspacePath);

        while (stack.Count > 0 && count < maxFiles)
        {
            string current = stack.Pop();
            IEnumerable<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(current);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (string directory in directories)
            {
                string name = Path.GetFileName(directory);
                if (ExcludedDirectoryNames.Contains(name))
                {
                    continue;
                }

                stack.Push(directory);
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(current);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (string file in files)
            {
                if (count >= maxFiles)
                {
                    yield break;
                }

                if (!IncludedExtensions.Contains(Path.GetExtension(file)))
                {
                    continue;
                }

                count++;
                yield return file;
            }
        }
    }
}
