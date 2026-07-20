namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Shared CLI discovery: find a path → unwrap once → stamp the result.
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
        IReadOnlyList<string> searchRoots = GetSearchRoots(additionalSearchPaths, layout, environment, includePath: true);
        IReadOnlyList<string> bundleRoots = GetSearchRoots(additionalSearchPaths, layout, environment, includePath: false);

        if (TryAbsolutePath(executable, environment, searchedPaths) is { } absolute)
        {
            return Complete(absolute, layout, environment, searchedPaths);
        }

        if (TryFindBundle(bundleRoots, layout, environment, searchedPaths) is { } bundle)
        {
            return ToResult(bundle, environment, searchedPaths);
        }

        if (FindInDirectories(searchRoots, candidateNames, environment, searchedPaths) is { } fromPath)
        {
            return Complete(fromPath, layout, environment, searchedPaths);
        }

        if (TryFindFallback(layout, candidateNames, environment, searchedPaths) is { } fallback)
        {
            return Complete(fallback, layout, environment, searchedPaths);
        }

        return Fail(layout.AgentDisplayName, executable, environment, searchedPaths);
    }

    private static ResolveResult Complete(
        string resolvedPath,
        IAgentCliInstallLayout layout,
        IExecutableEnvironment environment,
        List<string> searchedPaths)
    {
        string installDirectory = Path.GetDirectoryName(resolvedPath) ?? resolvedPath;
        AgentCliLaunchSpec launch =
            layout.TryResolveBundle(installDirectory, environment, searchedPaths)
            ?? new AgentCliLaunchSpec(resolvedPath, null);

        return ToResult(launch, environment, searchedPaths);
    }

    private static ResolveResult ToResult(
        AgentCliLaunchSpec launch,
        IExecutableEnvironment environment,
        List<string> searchedPaths) =>
        new(
            true,
            launch,
            null,
            environment.HostPlatform,
            AgentCliHostDetector.DetectInstallKind(launch.ExecutablePath, environment.HostPlatform),
            DistinctPaths(searchedPaths));

    private static ResolveResult Fail(
        string agentDisplayName,
        string executable,
        IExecutableEnvironment environment,
        List<string> searchedPaths)
    {
        IReadOnlyList<string> searched = DistinctPaths(searchedPaths);
        string message =
            $"Unable to locate {agentDisplayName} CLI executable '{executable}'. " +
            $"Ensure {agentDisplayName} is installed and on PATH, restart the Orchi API after installing, " +
            "or set the agent Executable setting to the full path. " +
            $"platform={environment.HostPlatform}; " +
            $"Searched: {string.Join("; ", searched)}";

        return new ResolveResult(
            false,
            null,
            message,
            environment.HostPlatform,
            AgentCliInstallKind.Unknown,
            searched);
    }

    private static string? TryAbsolutePath(
        string executable,
        IExecutableEnvironment environment,
        ICollection<string> searchedPaths)
    {
        if (!Path.IsPathRooted(executable))
        {
            return null;
        }

        string absolutePath = Path.GetFullPath(executable);
        searchedPaths.Add(absolutePath);
        return environment.FileExists(absolutePath) ? absolutePath : null;
    }

    private static AgentCliLaunchSpec? TryFindBundle(
        IEnumerable<string> directories,
        IAgentCliInstallLayout layout,
        IExecutableEnvironment environment,
        ICollection<string> searchedPaths)
    {
        foreach (string directory in directories)
        {
            AgentCliLaunchSpec? bundle = layout.TryResolveBundle(directory, environment, searchedPaths);
            if (bundle is not null)
            {
                return bundle;
            }
        }

        return null;
    }

    private static string? TryFindFallback(
        IAgentCliInstallLayout layout,
        IReadOnlyList<string> candidateNames,
        IExecutableEnvironment environment,
        ICollection<string> searchedPaths)
    {
        foreach (string fallbackPath in layout.GetFallbackPaths(environment, candidateNames))
        {
            searchedPaths.Add(fallbackPath);
            if (environment.FileExists(fallbackPath))
            {
                return fallbackPath;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> GetSearchRoots(
        IReadOnlyList<string> additionalSearchPaths,
        IAgentCliInstallLayout layout,
        IExecutableEnvironment environment,
        bool includePath)
    {
        var directories = new List<string>();
        AddExpanded(directories, additionalSearchPaths, environment);
        AddExpanded(directories, AgentCliKnownDirectories.For(environment), environment);
        AddExpanded(directories, layout.GetPreferredInstallDirectories(environment), environment);

        if (includePath)
        {
            AddExpanded(directories, environment.GetPathDirectories(), environment);
        }

        // npm global packages often live under the prefix parent of .../bin
        foreach (string directory in directories.ToList())
        {
            if (!directory.EndsWith($"{Path.DirectorySeparatorChar}bin", StringComparison.OrdinalIgnoreCase) &&
                !directory.EndsWith($"{Path.AltDirectorySeparatorChar}bin", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? parent = Path.GetDirectoryName(
                directory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrWhiteSpace(parent))
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
        var candidates = new List<string>();

        foreach (string directory in directories)
        {
            if (!environment.DirectoryExists(directory))
            {
                continue;
            }

            foreach (string candidateName in candidateNames)
            {
                AddCandidates(directory, candidateName, environment, searchedPaths, candidates);
            }
        }

        return SelectPreferredCandidate(candidates, environment);
    }

    private static void AddCandidates(
        string directory,
        string candidateName,
        IExecutableEnvironment environment,
        ICollection<string> searchedPaths,
        List<string> candidates)
    {
        if (Path.HasExtension(candidateName))
        {
            TryAddCandidate(Path.Combine(directory, candidateName), environment, searchedPaths, candidates);
            return;
        }

        if (!environment.IsWindows)
        {
            TryAddCandidate(Path.Combine(directory, candidateName), environment, searchedPaths, candidates);
            return;
        }

        foreach (string extension in WindowsPreferredExtensions
                     .Concat(environment.GetPathExtensions())
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            TryAddCandidate(Path.Combine(directory, candidateName + extension), environment, searchedPaths, candidates);
        }

        // Note extensionless npm shims as searched; they are not spawnable via CreateProcess.
        searchedPaths.Add(Path.Combine(directory, candidateName));
    }

    private static void TryAddCandidate(
        string fullPath,
        IExecutableEnvironment environment,
        ICollection<string> searchedPaths,
        List<string> candidates)
    {
        searchedPaths.Add(fullPath);
        if (IsSpawnableCandidate(fullPath, environment))
        {
            candidates.Add(fullPath);
        }
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
        return !string.IsNullOrEmpty(extension)
            && environment.GetPathExtensions().Contains(extension, StringComparer.OrdinalIgnoreCase);
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

    private static IReadOnlyList<string> DistinctPaths(IEnumerable<string> paths) =>
        paths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
}
