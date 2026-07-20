using Orchi.Api.Infrastructure.Agents.Cursor;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal static class CodexAgentExecutableResolver
{
    private static readonly string[] PreferredExtensions = [".exe", ".com", ".cmd", ".bat"];

    internal sealed record ResolveResult(bool Success, CodexAgentLaunchSpec? Launch, string? ErrorMessage);

    public static ResolveResult Resolve(CodexAgentOptions options) =>
        Resolve(options, ExecutableEnvironment.Current);

    internal static ResolveResult Resolve(CodexAgentOptions options, IExecutableEnvironment environment)
    {
        var searchedPaths = new List<string>();

        if (Path.IsPathRooted(options.Executable))
        {
            string absolutePath = Path.GetFullPath(options.Executable);
            searchedPaths.Add(absolutePath);

            if (environment.FileExists(absolutePath))
            {
                return Success(ResolveLaunchSpec(absolutePath, environment, searchedPaths));
            }
        }

        foreach (string installDirectory in GetInstallDirectories(options, environment))
        {
            CodexAgentLaunchSpec? nodeBundle = TryResolveNpmNodeBundle(installDirectory, environment, searchedPaths);
            if (nodeBundle is not null)
            {
                return Success(nodeBundle);
            }
        }

        string[] candidateNames = GetCandidateNames(options.Executable);
        IEnumerable<string> searchDirectories = GetSearchDirectories(options, environment);

        string? resolved = FindInDirectories(searchDirectories, candidateNames, environment, searchedPaths);
        if (resolved is not null)
        {
            string installDirectory = Path.GetDirectoryName(resolved) ?? resolved;
            CodexAgentLaunchSpec? nodeBundle = TryResolveNpmNodeBundle(installDirectory, environment, searchedPaths);
            if (nodeBundle is not null)
            {
                return Success(nodeBundle);
            }

            return Success(ResolveLaunchSpec(resolved, environment, searchedPaths));
        }

        foreach (string fallbackPath in GetWindowsFallbackPaths(environment, candidateNames))
        {
            searchedPaths.Add(fallbackPath);

            if (!environment.FileExists(fallbackPath))
            {
                continue;
            }

            string installDirectory = Path.GetDirectoryName(fallbackPath) ?? fallbackPath;
            CodexAgentLaunchSpec? nodeBundle = TryResolveNpmNodeBundle(installDirectory, environment, searchedPaths);
            if (nodeBundle is not null)
            {
                return Success(nodeBundle);
            }

            return Success(ResolveLaunchSpec(fallbackPath, environment, searchedPaths));
        }

        string message =
            $"Unable to locate Codex CLI executable '{options.Executable}'. " +
            "Ensure Codex is installed and on PATH, restart the Orchi API after installing, " +
            "or set Agents:Codex:Executable to the full path (e.g. %APPDATA%\\npm\\codex.cmd). " +
            $"Searched: {string.Join("; ", searchedPaths.Distinct(StringComparer.OrdinalIgnoreCase))}";

        return new ResolveResult(false, null, message);
    }

    internal static CodexAgentLaunchSpec? TryResolveNpmNodeBundle(
        string installDirectory,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths = null)
    {
        if (!environment.DirectoryExists(installDirectory))
        {
            return null;
        }

        string nodePath = Path.Combine(installDirectory, "node.exe");
        string codexJsPath = Path.Combine(
            installDirectory,
            "node_modules",
            "@openai",
            "codex",
            "bin",
            "codex.js");
        searchedPaths?.Add(nodePath);
        searchedPaths?.Add(codexJsPath);

        if (environment.FileExists(nodePath) && environment.FileExists(codexJsPath))
        {
            return new CodexAgentLaunchSpec(nodePath, codexJsPath);
        }

        return null;
    }

    private static ResolveResult Success(CodexAgentLaunchSpec launch) =>
        new(true, launch, null);

    private static CodexAgentLaunchSpec ResolveLaunchSpec(
        string executablePath,
        IExecutableEnvironment environment,
        ICollection<string> searchedPaths)
    {
        string installDirectory = Path.GetDirectoryName(executablePath) ?? executablePath;
        CodexAgentLaunchSpec? nodeBundle = TryResolveNpmNodeBundle(installDirectory, environment, searchedPaths);
        return nodeBundle ?? new CodexAgentLaunchSpec(executablePath, null);
    }

    private static IEnumerable<string> GetInstallDirectories(
        CodexAgentOptions options,
        IExecutableEnvironment environment)
    {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (options.AdditionalSearchPaths is { Length: > 0 })
        {
            foreach (string searchPath in options.AdditionalSearchPaths)
            {
                if (string.IsNullOrWhiteSpace(searchPath))
                {
                    continue;
                }

                directories.Add(environment.ExpandEnvironmentVariables(searchPath.Trim()));
            }
        }

        if (environment.IsWindows)
        {
            string? appData = environment.GetEnvironmentVariable("APPDATA");
            if (!string.IsNullOrWhiteSpace(appData))
            {
                directories.Add(Path.Combine(appData, "npm"));
            }

            string? programFiles = environment.GetEnvironmentVariable("ProgramFiles");
            if (!string.IsNullOrWhiteSpace(programFiles))
            {
                directories.Add(Path.Combine(programFiles, "nodejs"));
            }
        }

        foreach (string pathDirectory in environment.GetPathDirectories())
        {
            if (string.IsNullOrWhiteSpace(pathDirectory))
            {
                continue;
            }

            directories.Add(environment.ExpandEnvironmentVariables(pathDirectory.Trim()));
        }

        return directories;
    }

    private static string[] GetCandidateNames(string executable)
    {
        string trimmed = executable.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            trimmed = "codex";
        }

        string fileName = Path.GetFileName(trimmed);
        return Path.HasExtension(fileName)
            ? [fileName, Path.GetFileNameWithoutExtension(fileName)]
            : [fileName];
    }

    private static IEnumerable<string> GetSearchDirectories(
        CodexAgentOptions options,
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

        return SelectPreferredCandidate(candidates, environment);
    }

    private static IEnumerable<string> GetWindowsFallbackPaths(
        IExecutableEnvironment environment,
        IReadOnlyList<string> candidateNames)
    {
        if (!environment.IsWindows)
        {
            yield break;
        }

        string? appData = environment.GetEnvironmentVariable("APPDATA");
        if (!string.IsNullOrWhiteSpace(appData))
        {
            string npmDirectory = Path.Combine(appData, "npm");

            foreach (string candidateName in candidateNames)
            {
                string baseName = Path.HasExtension(candidateName)
                    ? Path.GetFileNameWithoutExtension(candidateName)
                    : candidateName;

                yield return Path.Combine(npmDirectory, baseName + ".cmd");
                yield return Path.Combine(npmDirectory, baseName + ".exe");
            }
        }
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

        string extension = Path.GetExtension(path);

        if (!string.IsNullOrEmpty(extension))
        {
            return true;
        }

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
