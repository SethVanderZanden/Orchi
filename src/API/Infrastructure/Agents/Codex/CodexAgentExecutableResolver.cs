namespace Orchi.Api.Infrastructure.Agents.Codex;

internal static class CodexAgentExecutableResolver
{
    private static readonly string[] PreferredExtensions = [".exe", ".cmd", ".bat", ".com"];

    internal sealed record ResolveResult(bool Success, string? ExecutablePath, string? ErrorMessage);

    public static ResolveResult Resolve(CodexAgentOptions options)
    {
        var searchedPaths = new List<string>();

        if (Path.IsPathRooted(options.Executable))
        {
            string absolutePath = Path.GetFullPath(options.Executable);
            searchedPaths.Add(absolutePath);

            if (File.Exists(absolutePath))
            {
                return new ResolveResult(true, absolutePath, null);
            }
        }

        string[] candidateNames = GetCandidateNames(options.Executable);

        foreach (string directory in GetSearchDirectories(options))
        {
            string? resolved = FindInDirectory(directory, candidateNames, searchedPaths);
            if (resolved is not null)
            {
                return new ResolveResult(true, resolved, null);
            }
        }

        string message =
            $"Unable to locate Codex CLI executable '{options.Executable}'. " +
            "Ensure Codex is installed and on PATH, restart the Orchi API after installing, " +
            "or set Agents:Codex:Executable to the full path. " +
            $"Searched: {string.Join("; ", searchedPaths.Distinct(StringComparer.OrdinalIgnoreCase))}";

        return new ResolveResult(false, null, message);
    }

    private static string[] GetCandidateNames(string executable)
    {
        string trimmed = executable.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            trimmed = "codex";
        }

        string fileName = Path.GetFileName(trimmed);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { fileName, "codex" };

        if (!Path.HasExtension(fileName) && OperatingSystem.IsWindows())
        {
            foreach (string extension in PreferredExtensions)
            {
                names.Add(fileName + extension);
                names.Add("codex" + extension);
            }
        }

        return names.ToArray();
    }

    private static IEnumerable<string> GetSearchDirectories(CodexAgentOptions options)
    {
        if (options.AdditionalSearchPaths is { Length: > 0 })
        {
            foreach (string path in options.AdditionalSearchPaths)
            {
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                {
                    yield return path;
                }
            }
        }

        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv))
        {
            yield break;
        }

        foreach (string segment in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmed = segment.Trim().Trim('"');
            if (Directory.Exists(trimmed))
            {
                yield return trimmed;
            }
        }
    }

    private static string? FindInDirectory(
        string directory,
        IReadOnlyList<string> candidateNames,
        ICollection<string> searchedPaths)
    {
        foreach (string candidate in candidateNames)
        {
            string fullPath = Path.Combine(directory, candidate);
            searchedPaths.Add(fullPath);

            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        return null;
    }
}
