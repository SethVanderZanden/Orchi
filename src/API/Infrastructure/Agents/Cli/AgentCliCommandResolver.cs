namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Shared CLI discovery suite used by every agent adapter.
/// Equivalent role to T3 Code's <c>@t3tools/shared/shell</c>
/// (<c>resolveCommandPath</c> / PATHEXT / known Windows CLI dirs).
/// </summary>
internal static class AgentCliCommandResolver
{
    private static readonly string[] PreferredExtensions = [".exe", ".com", ".cmd", ".bat"];

    internal sealed record ResolveResult(bool Success, AgentCliLaunchSpec? Launch, string? ErrorMessage);

    public static ResolveResult Resolve(
        string configuredExecutable,
        IReadOnlyList<string> additionalSearchPaths,
        IAgentCliInstallLayout layout) =>
        Resolve(configuredExecutable, additionalSearchPaths, layout, ExecutableEnvironment.Current);

    public static ResolveResult Resolve(
        string configuredExecutable,
        IReadOnlyList<string> additionalSearchPaths,
        IAgentCliInstallLayout layout,
        IExecutableEnvironment environment)
    {
        var searchedPaths = new List<string>();
        string executable = string.IsNullOrWhiteSpace(configuredExecutable)
            ? layout.GetCandidateNames("").FirstOrDefault() ?? "agent"
            : configuredExecutable.Trim();

        if (Path.IsPathRooted(executable))
        {
            string absolutePath = Path.GetFullPath(executable);
            searchedPaths.Add(absolutePath);

            if (environment.FileExists(absolutePath))
            {
                string installDirectory = Path.GetDirectoryName(absolutePath) ?? absolutePath;
                AgentCliLaunchSpec? bundle = layout.TryResolveBundle(installDirectory, environment, searchedPaths);
                if (bundle is not null)
                {
                    return Success(bundle);
                }

                return Success(new AgentCliLaunchSpec(absolutePath, null));
            }
        }

        foreach (string installDirectory in GetInstallDirectories(additionalSearchPaths, layout, environment))
        {
            AgentCliLaunchSpec? bundle = layout.TryResolveBundle(installDirectory, environment, searchedPaths);
            if (bundle is not null)
            {
                return Success(bundle);
            }
        }

        string[] candidateNames = layout.GetCandidateNames(executable);
        IEnumerable<string> searchDirectories = GetSearchDirectories(additionalSearchPaths, environment);

        string? resolved = FindInDirectories(searchDirectories, candidateNames, environment, searchedPaths);
        if (resolved is not null)
        {
            string installDirectory = Path.GetDirectoryName(resolved) ?? resolved;
            AgentCliLaunchSpec? bundle = layout.TryResolveBundle(installDirectory, environment, searchedPaths);
            if (bundle is not null)
            {
                return Success(bundle);
            }

            return Success(new AgentCliLaunchSpec(resolved, null));
        }

        foreach (string fallbackPath in layout.GetWindowsFallbackPaths(environment, candidateNames))
        {
            searchedPaths.Add(fallbackPath);

            if (!environment.FileExists(fallbackPath))
            {
                continue;
            }

            string installDirectory = Path.GetDirectoryName(fallbackPath) ?? fallbackPath;
            AgentCliLaunchSpec? bundle = layout.TryResolveBundle(installDirectory, environment, searchedPaths);
            if (bundle is not null)
            {
                return Success(bundle);
            }

            return Success(new AgentCliLaunchSpec(fallbackPath, null));
        }

        string message =
            $"Unable to locate {layout.AgentDisplayName} CLI executable '{executable}'. " +
            $"Ensure {layout.AgentDisplayName} is installed and on PATH, restart the Orchi API after installing, " +
            "or set the agent Executable setting to the full path. " +
            $"Searched: {string.Join("; ", searchedPaths.Distinct(StringComparer.OrdinalIgnoreCase))}";

        return new ResolveResult(false, null, message);
    }

    private static ResolveResult Success(AgentCliLaunchSpec launch) =>
        new(true, launch, null);

    private static IEnumerable<string> GetInstallDirectories(
        IReadOnlyList<string> additionalSearchPaths,
        IAgentCliInstallLayout layout,
        IExecutableEnvironment environment)
    {
        var directories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string searchPath in additionalSearchPaths)
        {
            if (string.IsNullOrWhiteSpace(searchPath))
            {
                continue;
            }

            directories.Add(environment.ExpandEnvironmentVariables(searchPath.Trim()));
        }

        foreach (string directory in layout.GetPreferredInstallDirectories(environment))
        {
            if (!string.IsNullOrWhiteSpace(directory))
            {
                directories.Add(directory);
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

    private static IEnumerable<string> GetSearchDirectories(
        IReadOnlyList<string> additionalSearchPaths,
        IExecutableEnvironment environment)
    {
        var directories = new List<string>();
        directories.AddRange(additionalSearchPaths);
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

                    if (IsWindowsExecutableCandidate(fullPath, environment))
                    {
                        candidates.Add(fullPath);
                    }

                    continue;
                }

                foreach (string extension in PreferredExtensions.Concat(pathExtensions).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    string fullPath = Path.Combine(directory, candidateName + extension);
                    searchedPaths.Add(fullPath);

                    if (IsWindowsExecutableCandidate(fullPath, environment))
                    {
                        candidates.Add(fullPath);
                    }
                }

                // Extensionless shims exist on Windows npm installs but are not spawnable
                // via CreateProcess (same rule as T3 shell.isExecutableFile).
                string extensionless = Path.Combine(directory, candidateName);
                searchedPaths.Add(extensionless);
                if (!environment.IsWindows && environment.FileExists(extensionless))
                {
                    candidates.Add(extensionless);
                }
            }
        }

        return SelectPreferredCandidate(candidates, environment);
    }

    private static bool IsWindowsExecutableCandidate(string fullPath, IExecutableEnvironment environment)
    {
        if (!environment.FileExists(fullPath))
        {
            return false;
        }

        if (!environment.IsWindows)
        {
            return true;
        }

        string extension = Path.GetExtension(fullPath);
        if (string.IsNullOrEmpty(extension))
        {
            return false;
        }

        return environment.GetPathExtensions()
            .Contains(extension, StringComparer.OrdinalIgnoreCase);
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
            .OrderBy(path => GetExtensionPriority(Path.GetExtension(path), environment))
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .First();
    }

    private static int GetExtensionPriority(string extension, IExecutableEnvironment environment)
    {
        if (string.IsNullOrEmpty(extension))
        {
            return environment.IsWindows ? 10 : 0;
        }

        return extension.ToLowerInvariant() switch
        {
            ".exe" => 0,
            ".com" => 1,
            ".cmd" => 2,
            ".bat" => 3,
            _ => 4
        };
    }
}
