using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Cursor;

internal sealed class CursorCliInstallLayout : IAgentCliInstallLayout
{
    private static readonly string[] AliasNames = ["agent", "cursor-agent"];

    private static readonly System.Text.RegularExpressions.Regex VersionDirectoryPattern =
        new(@"^\d{4}\.\d{1,2}\.\d{1,2}(-\d{2}-\d{2}-\d{2})?-[a-f0-9]+$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    public string AgentDisplayName => "Cursor";

    public string[] GetCandidateNames(string configuredExecutable)
    {
        if (string.IsNullOrWhiteSpace(configuredExecutable))
        {
            return AliasNames;
        }

        string name = Path.GetFileNameWithoutExtension(configuredExecutable);
        return AliasNames.Contains(name, StringComparer.OrdinalIgnoreCase)
            ? AliasNames
            : [configuredExecutable, name];
    }

    public IEnumerable<string> GetPreferredInstallDirectories(IExecutableEnvironment environment)
    {
        switch (environment.HostPlatform)
        {
            case AgentCliHostPlatform.Windows:
            {
                string? localAppData = environment.GetEnvironmentVariable("LOCALAPPDATA");
                if (!string.IsNullOrWhiteSpace(localAppData))
                {
                    yield return Path.Combine(localAppData, "cursor-agent");
                }

                break;
            }
            case AgentCliHostPlatform.MacOS:
            case AgentCliHostPlatform.Linux:
            {
                // Best-effort Unix layout guesses until verified against a real Cursor CLI
                // install on macOS/Linux (official paths may differ by installer version).
                string? home = environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrWhiteSpace(home))
                {
                    yield return Path.Combine(home, ".local", "share", "cursor-agent");
                    yield return Path.Combine(home, ".cursor-agent");
                }

                break;
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
                yield return expanded;
            }
        }
    }

    public IEnumerable<string> GetFallbackPaths(
        IExecutableEnvironment environment,
        IReadOnlyList<string> candidateNames)
    {
        foreach (string installDirectory in GetPreferredInstallDirectories(environment))
        {
            foreach (string candidateName in candidateNames)
            {
                string baseName = Path.HasExtension(candidateName)
                    ? Path.GetFileNameWithoutExtension(candidateName)
                    : candidateName;

                if (environment.HostPlatform == AgentCliHostPlatform.Windows)
                {
                    yield return Path.Combine(installDirectory, baseName + ".exe");
                    yield return Path.Combine(installDirectory, "cursor-agent.exe");
                    yield return Path.Combine(installDirectory, baseName + ".cmd");
                    yield return Path.Combine(installDirectory, "cursor-agent.cmd");
                }
                else
                {
                    yield return Path.Combine(installDirectory, baseName);
                    yield return Path.Combine(installDirectory, "cursor-agent");
                    yield return Path.Combine(installDirectory, "agent");
                }
            }
        }
    }

    public AgentCliLaunchSpec? TryResolveBundle(
        string installDirectory,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths = null)
    {
        if (!environment.DirectoryExists(installDirectory))
        {
            return null;
        }

        string nodeFileName = environment.HostPlatform == AgentCliHostPlatform.Windows ? "node.exe" : "node";
        string coLocatedNode = Path.Combine(installDirectory, nodeFileName);
        string coLocatedIndex = Path.Combine(installDirectory, "index.js");
        searchedPaths?.Add(coLocatedNode);
        searchedPaths?.Add(coLocatedIndex);

        if (environment.FileExists(coLocatedNode) && environment.FileExists(coLocatedIndex))
        {
            return new AgentCliLaunchSpec(coLocatedNode, coLocatedIndex);
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

        string nodePath = Path.Combine(latestVersionDirectory, nodeFileName);
        string indexPath = Path.Combine(latestVersionDirectory, "index.js");
        searchedPaths?.Add(nodePath);
        searchedPaths?.Add(indexPath);

        if (!environment.FileExists(nodePath) || !environment.FileExists(indexPath))
        {
            return null;
        }

        return new AgentCliLaunchSpec(nodePath, indexPath);
    }

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
}
