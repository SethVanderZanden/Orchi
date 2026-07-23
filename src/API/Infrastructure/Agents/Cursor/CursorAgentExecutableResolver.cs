using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

/// <summary>
/// Resolves Cursor CLI. Prefers the local <c>cursor-agent</c> node bundle (avoids
/// PowerShell <c>.cmd</c> shims that can corrupt prompts), then falls back to PATH
/// via <see cref="AgentCliPathLocator"/>.
/// </summary>
internal static class CursorAgentExecutableResolver
{
    private static readonly string[] AliasNames = ["agent", "cursor-agent"];

    private static readonly System.Text.RegularExpressions.Regex VersionDirectoryPattern =
        new(@"^\d{4}\.\d{1,2}\.\d{1,2}(-\d{2}-\d{2}-\d{2})?-[a-f0-9]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    internal sealed record ResolveResult(bool Success, AgentLaunchSpec? Launch, string? ErrorMessage);

    public static ResolveResult Resolve(CursorAgentOptions options) =>
        Resolve(options, ExecutableEnvironment.Current);

    internal static ResolveResult Resolve(CursorAgentOptions options, IExecutableEnvironment environment)
    {
        var searchedPaths = new List<string>();

        if (AgentCliPathLocator.TryResolveAbsolute(
                options.Executable,
                environment,
                searchedPaths,
                out string? absolutePath) &&
            absolutePath is not null)
        {
            return Success(new AgentLaunchSpec(absolutePath, null));
        }

        foreach (string installDirectory in GetInstallDirectories(options, environment))
        {
            AgentLaunchSpec? nodeBundle = TryResolveNodeBundle(installDirectory, environment, searchedPaths);
            if (nodeBundle is not null)
            {
                return Success(nodeBundle);
            }
        }

        string[] candidateNames = GetCandidateNames(options.Executable);

        string? resolved = AgentCliPathLocator.FindCommand(
            AgentCliPathLocator.GetSearchDirectories(options.AdditionalSearchPaths, environment),
            candidateNames,
            environment,
            searchedPaths);
        if (resolved is not null)
        {
            string installDirectory = Path.GetDirectoryName(resolved) ?? resolved;
            AgentLaunchSpec? nodeBundle = TryResolveNodeBundle(installDirectory, environment, searchedPaths);
            if (nodeBundle is not null)
            {
                return Success(nodeBundle);
            }

            return Success(new AgentLaunchSpec(resolved, null));
        }

        foreach (string fallbackPath in GetWindowsFallbackPaths(environment, candidateNames))
        {
            searchedPaths.Add(fallbackPath);

            if (!environment.FileExists(fallbackPath))
            {
                continue;
            }

            string installDirectory = Path.GetDirectoryName(fallbackPath) ?? fallbackPath;
            AgentLaunchSpec? nodeBundle = TryResolveNodeBundle(installDirectory, environment, searchedPaths);
            if (nodeBundle is not null)
            {
                return Success(nodeBundle);
            }

            return Success(new AgentLaunchSpec(fallbackPath, null));
        }

        string message =
            $"Unable to locate Cursor CLI executable '{options.Executable}'. " +
            "Ensure the Cursor agent is installed, restart the Orchi API after installing, " +
            "or set Agents:Cursor:Executable to the full path (e.g. %LOCALAPPDATA%\\cursor-agent\\agent.exe). " +
            $"Searched: {string.Join("; ", searchedPaths.Distinct(StringComparer.OrdinalIgnoreCase))}";

        return new ResolveResult(false, null, message);
    }

    internal static AgentLaunchSpec? TryResolveNodeBundle(
        string installDirectory,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths = null)
    {
        if (!environment.DirectoryExists(installDirectory))
        {
            return null;
        }

        string coLocatedNode = Path.Combine(installDirectory, "node.exe");
        string coLocatedIndex = Path.Combine(installDirectory, "index.js");
        searchedPaths?.Add(coLocatedNode);
        searchedPaths?.Add(coLocatedIndex);

        if (environment.FileExists(coLocatedNode) && environment.FileExists(coLocatedIndex))
        {
            return new AgentLaunchSpec(coLocatedNode, coLocatedIndex);
        }

        string versionsDirectory = Path.Combine(installDirectory, "versions");
        searchedPaths?.Add(versionsDirectory);

        if (!environment.DirectoryExists(versionsDirectory))
        {
            return null;
        }

        string? latestVersionDirectory = environment.GetDirectories(versionsDirectory)
            .Where(path => VersionDirectoryPattern.IsMatch(Path.GetFileName(path)))
            .OrderByDescending(ParseVersionDirectoryKey)
            .FirstOrDefault();

        if (latestVersionDirectory is null)
        {
            return null;
        }

        string nodePath = Path.Combine(latestVersionDirectory, "node.exe");
        string indexPath = Path.Combine(latestVersionDirectory, "index.js");
        searchedPaths?.Add(nodePath);
        searchedPaths?.Add(indexPath);

        if (!environment.FileExists(nodePath) || !environment.FileExists(indexPath))
        {
            return null;
        }

        return new AgentLaunchSpec(nodePath, indexPath);
    }

    private static ResolveResult Success(AgentLaunchSpec launch) =>
        new(true, launch, null);

    private static int ParseVersionDirectoryKey(string versionDirectory)
    {
        string versionString = Path.GetFileName(versionDirectory);
        string datePart = versionString.Split('-')[0];
        string[] parts = datePart.Split('.');

        if (parts.Length != 3 ||
            !int.TryParse(parts[0], out int year) ||
            !int.TryParse(parts[1], out int month) ||
            !int.TryParse(parts[2], out int day))
        {
            return 0;
        }

        return year * 10_000 + month * 100 + day;
    }

    private static IEnumerable<string> GetInstallDirectories(
        CursorAgentOptions options,
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
            string? localAppData = environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrWhiteSpace(localAppData))
            {
                directories.Add(Path.Combine(localAppData, "cursor-agent"));
            }
        }

        foreach (string pathDirectory in environment.GetPathDirectories())
        {
            if (string.IsNullOrWhiteSpace(pathDirectory))
            {
                continue;
            }

            string expanded = environment.ExpandEnvironmentVariables(pathDirectory.Trim());
            if (expanded.EndsWith("cursor-agent", StringComparison.OrdinalIgnoreCase))
            {
                directories.Add(expanded);
            }
        }

        return directories;
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
}
