namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Shared CLI discovery suite used by every agent adapter.
/// Pipeline: find a candidate path → classify install kind → unwrap once → stamp result.
/// </summary>
internal static class AgentCliCommandResolver
{
    private static readonly string[] WindowsPreferredExtensions = [".exe", ".com", ".cmd", ".bat"];

    internal sealed record ResolveResult(
        bool Success,
        AgentCliLaunchSpec? Launch,
        string? ErrorMessage,
        AgentCliHostPlatform HostPlatform,
        AgentCliInstallKind InstallKind,
        IReadOnlyList<string> SearchedPaths)
    {
        public string LaunchKind => Launch?.LaunchKind ?? "none";
    }

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

        string[] candidateNames = layout.GetCandidateNames(executable);
        IReadOnlyList<string> searchDirectories = CollectSearchDirectories(
            additionalSearchPaths,
            layout,
            environment);

        // 1) Absolute configured path
        if (Path.IsPathRooted(executable))
        {
            string absolutePath = Path.GetFullPath(executable);
            searchedPaths.Add(absolutePath);

            if (environment.FileExists(absolutePath))
            {
                return Finish(absolutePath, layout, environment, searchedPaths);
            }
        }

        // 2) Prefer native / node bundles in known + agent install dirs (before PATH shims)
        foreach (string installDirectory in CollectBundleProbeDirectories(additionalSearchPaths, layout, environment))
        {
            AgentCliLaunchSpec? bundle = layout.TryResolveBundle(installDirectory, environment, searchedPaths);
            if (bundle is not null)
            {
                return Success(bundle, environment, searchedPaths);
            }
        }

        // 3) PATH / known-dir file search
        string? resolved = FindInDirectories(searchDirectories, candidateNames, environment, searchedPaths);
        if (resolved is not null)
        {
            return Finish(resolved, layout, environment, searchedPaths);
        }

        // 4) Last-resort absolute fallbacks
        foreach (string fallbackPath in layout.GetFallbackPaths(environment, candidateNames))
        {
            searchedPaths.Add(fallbackPath);

            if (!environment.FileExists(fallbackPath))
            {
                continue;
            }

            return Finish(fallbackPath, layout, environment, searchedPaths);
        }

        string message =
            $"Unable to locate {layout.AgentDisplayName} CLI executable '{executable}'. " +
            $"Ensure {layout.AgentDisplayName} is installed and on PATH, restart the Orchi API after installing, " +
            "or set the agent Executable setting to the full path. " +
            $"platform={environment.HostPlatform}; " +
            $"Searched: {string.Join("; ", searchedPaths.Distinct(StringComparer.OrdinalIgnoreCase))}";

        return new ResolveResult(
            false,
            null,
            message,
            environment.HostPlatform,
            AgentCliInstallKind.Unknown,
            searchedPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static ResolveResult Finish(
        string resolvedPath,
        IAgentCliInstallLayout layout,
        IExecutableEnvironment environment,
        List<string> searchedPaths)
    {
        string installDirectory = Path.GetDirectoryName(resolvedPath) ?? resolvedPath;
        AgentCliLaunchSpec? bundle = layout.TryResolveBundle(installDirectory, environment, searchedPaths);
        AgentCliLaunchSpec launch = bundle ?? new AgentCliLaunchSpec(resolvedPath, null);
        return Success(launch, environment, searchedPaths);
    }

    private static ResolveResult Success(
        AgentCliLaunchSpec launch,
        IExecutableEnvironment environment,
        List<string> searchedPaths)
    {
        AgentCliInstallKind installKind = AgentCliHostDetector.DetectInstallKind(
            launch.ExecutablePath,
            environment.HostPlatform);

        return new ResolveResult(
            true,
            launch,
            null,
            environment.HostPlatform,
            installKind,
            searchedPaths.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static IReadOnlyList<string> CollectSearchDirectories(
        IReadOnlyList<string> additionalSearchPaths,
        IAgentCliInstallLayout layout,
        IExecutableEnvironment environment)
    {
        var directories = new List<string>();

        AddExpanded(directories, additionalSearchPaths, environment);
        AddExpanded(directories, AgentCliKnownDirectories.For(environment), environment);
        AddExpanded(directories, layout.GetPreferredInstallDirectories(environment), environment);
        AddExpanded(directories, environment.GetPathDirectories(), environment);

        return directories
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> CollectBundleProbeDirectories(
        IReadOnlyList<string> additionalSearchPaths,
        IAgentCliInstallLayout layout,
        IExecutableEnvironment environment)
    {
        var directories = new List<string>();

        AddExpanded(directories, additionalSearchPaths, environment);
        AddExpanded(directories, AgentCliKnownDirectories.For(environment), environment);
        AddExpanded(directories, layout.GetPreferredInstallDirectories(environment), environment);

        // npm global often stores packages under the prefix parent of .../bin
        foreach (string directory in directories.ToList())
        {
            string? parent = Path.GetDirectoryName(directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrWhiteSpace(parent) &&
                directory.EndsWith($"{Path.DirectorySeparatorChar}bin", StringComparison.OrdinalIgnoreCase))
            {
                directories.Add(parent);
            }
        }

        return directories
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddExpanded(
        List<string> directories,
        IEnumerable<string> source,
        IExecutableEnvironment environment)
    {
        foreach (string directory in source)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            directories.Add(environment.ExpandEnvironmentVariables(directory.Trim()));
        }
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

                    if (IsSpawnableCandidate(fullPath, environment))
                    {
                        candidates.Add(fullPath);
                    }

                    continue;
                }

                if (environment.IsWindows)
                {
                    foreach (string extension in WindowsPreferredExtensions
                                 .Concat(pathExtensions)
                                 .Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        string fullPath = Path.Combine(directory, candidateName + extension);
                        searchedPaths.Add(fullPath);

                        if (IsSpawnableCandidate(fullPath, environment))
                        {
                            candidates.Add(fullPath);
                        }
                    }

                    // Extensionless Windows npm shims are not CreateProcess-spawnable.
                    searchedPaths.Add(Path.Combine(directory, candidateName));
                    continue;
                }

                string extensionless = Path.Combine(directory, candidateName);
                searchedPaths.Add(extensionless);
                if (environment.FileExists(extensionless))
                {
                    candidates.Add(extensionless);
                }
            }
        }

        return SelectPreferredCandidate(candidates, environment);
    }

    private static bool IsSpawnableCandidate(string fullPath, IExecutableEnvironment environment)
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
