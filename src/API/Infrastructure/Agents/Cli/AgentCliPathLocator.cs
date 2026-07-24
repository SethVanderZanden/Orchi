namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Shared PATH / PATHEXT command lookup for agent CLIs.
/// Prefer this over digging into nested vendor binaries under node_modules.
/// </summary>
internal static class AgentCliPathLocator
{
    private static readonly string[] PreferredExtensions = [".exe", ".com", ".cmd", ".bat"];

    /// <summary>
    /// Finds a launchable command in the given directories (typically PATH + extras).
    /// Prefers <c>.exe</c> over <c>.cmd</c>; skips extensionless bash shims on Windows.
    /// </summary>
    public static string? FindCommand(
        IEnumerable<string> directories,
        IReadOnlyList<string> candidateNames,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths = null)
    {
        IReadOnlyList<string> pathExtensions = environment.GetPathExtensions();
        var candidates = new List<string>();

        foreach (string directory in directories)
        {
            if (!environment.DirectoryExists(directory))
            {
                continue;
            }

            foreach (string candidateName in candidateNames)
            {
                if (Path.HasExtension(candidateName))
                {
                    string fullPath = Path.Combine(directory, candidateName);
                    searchedPaths?.Add(fullPath);

                    if (environment.FileExists(fullPath))
                    {
                        candidates.Add(fullPath);
                    }

                    continue;
                }

                foreach (string extension in PreferredExtensions
                             .Concat(pathExtensions)
                             .Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    string fullPath = Path.Combine(directory, candidateName + extension);
                    searchedPaths?.Add(fullPath);

                    if (environment.FileExists(fullPath))
                    {
                        candidates.Add(fullPath);
                    }
                }
            }
        }

        return SelectPreferredCandidate(candidates, environment);
    }

    /// <summary>
    /// Expands <paramref name="additionalSearchPaths"/> then merges PATH directories.
    /// </summary>
    public static IEnumerable<string> GetSearchDirectories(
        IReadOnlyList<string>? additionalSearchPaths,
        IExecutableEnvironment environment)
    {
        var directories = new List<string>();

        if (additionalSearchPaths is { Count: > 0 })
        {
            directories.AddRange(additionalSearchPaths);
        }

        directories.AddRange(environment.GetPathDirectories());

        return directories
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Select(directory => environment.ExpandEnvironmentVariables(directory.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public static string[] CandidateNamesFromExecutable(string executable, string defaultName)
    {
        string trimmed = executable.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            trimmed = defaultName;
        }

        string fileName = Path.GetFileName(trimmed);
        return Path.HasExtension(fileName)
            ? [fileName, Path.GetFileNameWithoutExtension(fileName)]
            : [fileName];
    }

    public static bool TryResolveAbsolute(
        string executable,
        IExecutableEnvironment environment,
        ICollection<string> searchedPaths,
        out string? absolutePath)
    {
        absolutePath = null;

        if (!Path.IsPathRooted(executable))
        {
            return false;
        }

        string fullPath = Path.GetFullPath(executable);
        searchedPaths.Add(fullPath);

        if (!environment.FileExists(fullPath))
        {
            return false;
        }

        absolutePath = fullPath;
        return true;
    }

    private static string? SelectPreferredCandidate(
        IReadOnlyList<string> candidates,
        IExecutableEnvironment environment)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates
            .Where(path => IsLaunchableCandidate(path, environment))
            .OrderBy(path => GetExtensionPriority(Path.GetExtension(path)))
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private static bool IsLaunchableCandidate(string path, IExecutableEnvironment environment)
    {
        if (!environment.FileExists(path))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(Path.GetExtension(path)))
        {
            return true;
        }

        // Extensionless bash shims are not Process.Start-able on Windows.
        return !environment.IsWindows;
    }

    private static int GetExtensionPriority(string extension) =>
        extension.ToLowerInvariant() switch
        {
            ".exe" => 0,
            ".com" => 1,
            ".cmd" => 2,
            ".bat" => 3,
            _ => 4
        };
}
