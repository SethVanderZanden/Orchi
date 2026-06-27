namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal static class CursorAgentExecutableResolver
{
    private static readonly string[] AliasNames = ["agent", "cursor-agent"];

    private static readonly string[] PreferredExtensions = [".exe", ".com", ".cmd", ".bat"];

    internal sealed record ResolveResult(bool Success, string? ExecutablePath, string? ErrorMessage);

    public static ResolveResult Resolve(CursorAgentOptions options) =>
        Resolve(options, ExecutableEnvironment.Current);

    internal static ResolveResult Resolve(CursorAgentOptions options, IExecutableEnvironment environment)
    {
        var searchedPaths = new List<string>();

        if (Path.IsPathRooted(options.Executable))
        {
            string absolutePath = Path.GetFullPath(options.Executable);
            searchedPaths.Add(absolutePath);

            if (environment.FileExists(absolutePath))
            {
                return new ResolveResult(true, absolutePath, null);
            }
        }

        string[] candidateNames = GetCandidateNames(options.Executable);
        IEnumerable<string> searchDirectories = GetSearchDirectories(options, environment);

        string? resolved = FindInDirectories(searchDirectories, candidateNames, environment, searchedPaths);
        if (resolved is not null)
        {
            return new ResolveResult(true, resolved, null);
        }

        foreach (string fallbackPath in GetWindowsFallbackPaths(environment, candidateNames))
        {
            searchedPaths.Add(fallbackPath);

            if (environment.FileExists(fallbackPath))
            {
                return new ResolveResult(true, fallbackPath, null);
            }
        }

        string message =
            $"Unable to locate Cursor CLI executable '{options.Executable}'. " +
            "Ensure the Cursor agent is installed, restart the Orchi API after installing, " +
            "or set Agents:Cursor:Executable to the full path (e.g. %LOCALAPPDATA%\\cursor-agent\\agent.exe). " +
            $"Searched: {string.Join("; ", searchedPaths.Distinct(StringComparer.OrdinalIgnoreCase))}";

        return new ResolveResult(false, null, message);
    }

    private static string[] GetCandidateNames(string executable)
    {
        if (string.IsNullOrWhiteSpace(executable))
        {
            return AliasNames;
        }

        string name = Path.GetFileNameWithoutExtension(executable);
        return AliasNames.Contains(name, StringComparer.OrdinalIgnoreCase)
            ? AliasNames
            : [executable, name];
    }

    private static IEnumerable<string> GetSearchDirectories(
        CursorAgentOptions options,
        IExecutableEnvironment environment)
    {
        var directories = new List<string>();

        if (options.AdditionalSearchPaths is { Length: > 0 })
        {
            directories.AddRange(options.AdditionalSearchPaths);
        }

        directories.AddRange(environment.GetPathDirectories());

        return directories
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Select(directory => environment.ExpandEnvironmentVariables(directory.Trim()))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string? FindInDirectories(
        IEnumerable<string> directories,
        IReadOnlyList<string> candidateNames,
        IExecutableEnvironment environment,
        ICollection<string> searchedPaths)
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
                    searchedPaths.Add(fullPath);

                    if (environment.FileExists(fullPath))
                    {
                        candidates.Add(fullPath);
                    }

                    continue;
                }

                foreach (string extension in PreferredExtensions.Concat(pathExtensions).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    string fullPath = Path.Combine(directory, candidateName + extension);
                    searchedPaths.Add(fullPath);

                    if (environment.FileExists(fullPath))
                    {
                        candidates.Add(fullPath);
                    }
                }
            }
        }

        return SelectPreferredCandidate(candidates);
    }

    private static IEnumerable<string> GetWindowsFallbackPaths(
        IExecutableEnvironment environment,
        IReadOnlyList<string> candidateNames)
    {
        if (!environment.IsWindows)
        {
            yield break;
        }

        string? localAppData = environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (string.IsNullOrWhiteSpace(localAppData))
        {
            yield break;
        }

        string installDirectory = Path.Combine(localAppData, "cursor-agent");

        foreach (string candidateName in candidateNames)
        {
            string baseName = Path.HasExtension(candidateName)
                ? Path.GetFileNameWithoutExtension(candidateName)
                : candidateName;

            yield return Path.Combine(installDirectory, baseName + ".exe");
            yield return Path.Combine(installDirectory, "cursor-agent.exe");
            yield return Path.Combine(installDirectory, baseName + ".cmd");
            yield return Path.Combine(installDirectory, "cursor-agent.cmd");
        }
    }

    private static string? SelectPreferredCandidate(IReadOnlyList<string> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates
            .OrderBy(path => GetExtensionPriority(Path.GetExtension(path)))
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .First();
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

internal interface IExecutableEnvironment
{
    bool IsWindows { get; }

    string? GetEnvironmentVariable(string name);

    string ExpandEnvironmentVariables(string value);

    bool FileExists(string path);

    bool DirectoryExists(string path);

    IReadOnlyList<string> GetPathDirectories();

    IReadOnlyList<string> GetPathExtensions();
}

internal sealed class ExecutableEnvironment : IExecutableEnvironment
{
    public static IExecutableEnvironment Current { get; } = new ExecutableEnvironment();

    public bool IsWindows => OperatingSystem.IsWindows();

    public string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);

    public string ExpandEnvironmentVariables(string value) => Environment.ExpandEnvironmentVariables(value);

    public bool FileExists(string path) => File.Exists(path);

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IReadOnlyList<string> GetPathDirectories()
    {
        var directories = new List<string>();

        AddPathDirectories(directories, Environment.GetEnvironmentVariable("PATH"));

        if (IsWindows)
        {
            AddPathDirectories(directories, Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User));
            AddPathDirectories(directories, Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine));
        }

        return directories
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<string> GetPathExtensions()
    {
        string? pathExt = Environment.GetEnvironmentVariable("PATHEXT");

        if (string.IsNullOrWhiteSpace(pathExt))
        {
            return [".COM", ".EXE", ".BAT", ".CMD"];
        }

        return pathExt
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddPathDirectories(ICollection<string> directories, string? pathValue)
    {
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return;
        }

        foreach (string directory in pathValue.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            directories.Add(directory);
        }
    }
}
