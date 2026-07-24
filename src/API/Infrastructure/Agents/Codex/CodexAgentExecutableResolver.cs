using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Codex;

/// <summary>
/// Resolves Codex the standard way: PATH shortcut first (<c>codex.exe</c> / <c>codex.cmd</c>),
/// then known installer locations. Does not dig into nested npm vendor binaries.
/// </summary>
internal static class CodexAgentExecutableResolver
{
    internal sealed record ResolveResult(bool Success, AgentLaunchSpec? Launch, string? ErrorMessage);

    public static ResolveResult Resolve(CodexAgentOptions options) =>
        Resolve(options, ExecutableEnvironment.Current);

    internal static ResolveResult Resolve(CodexAgentOptions options, IExecutableEnvironment environment)
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

        string[] candidateNames = AgentCliPathLocator.CandidateNamesFromExecutable(options.Executable, "codex");

        string? resolved = AgentCliPathLocator.FindCommand(
            AgentCliPathLocator.GetSearchDirectories(options.AdditionalSearchPaths, environment),
            candidateNames,
            environment,
            searchedPaths);
        if (resolved is not null)
        {
            return Success(new AgentLaunchSpec(resolved, null));
        }

        foreach (string installDirectory in GetKnownInstallDirectories(environment))
        {
            string coLocatedExe = Path.Combine(installDirectory, "codex.exe");
            searchedPaths.Add(coLocatedExe);

            if (environment.FileExists(coLocatedExe))
            {
                return Success(new AgentLaunchSpec(coLocatedExe, null));
            }
        }

        foreach (string fallbackPath in GetWindowsFallbackPaths(environment, candidateNames))
        {
            searchedPaths.Add(fallbackPath);

            if (environment.FileExists(fallbackPath))
            {
                return Success(new AgentLaunchSpec(fallbackPath, null));
            }
        }

        foreach (string installDirectory in GetNpmCandidateDirectories(options, environment))
        {
            AgentLaunchSpec? nodeBundle = TryResolveNpmNodeBundle(installDirectory, environment, searchedPaths);
            if (nodeBundle is not null)
            {
                return Success(nodeBundle);
            }
        }

        string message =
            $"Unable to locate Codex CLI executable '{options.Executable}'. " +
            "Ensure Codex is installed and on PATH, restart the Orchi API after installing, " +
            "or set Agents:Codex:Executable to the full path " +
            "(e.g. %LOCALAPPDATA%\\Programs\\OpenAI\\Codex\\bin\\codex.exe or %APPDATA%\\npm\\codex.cmd). " +
            $"Searched: {string.Join("; ", searchedPaths.Distinct(StringComparer.OrdinalIgnoreCase))}";

        return new ResolveResult(false, null, message);
    }

    internal static AgentLaunchSpec? TryResolveNpmNodeBundle(
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
            return new AgentLaunchSpec(nodePath, codexJsPath);
        }

        return TryResolveSplitNpmNodeBundle(installDirectory, environment, searchedPaths);
    }

    private static AgentLaunchSpec? TryResolveSplitNpmNodeBundle(
        string installDirectory,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths)
    {
        if (!environment.IsWindows)
        {
            return null;
        }

        string codexJsPath = Path.Combine(
            installDirectory,
            "node_modules",
            "@openai",
            "codex",
            "bin",
            "codex.js");
        searchedPaths?.Add(codexJsPath);

        if (!environment.FileExists(codexJsPath))
        {
            return null;
        }

        foreach (string nodeDirectory in GetNodeInstallDirectories(environment))
        {
            string nodePath = Path.Combine(nodeDirectory, "node.exe");
            searchedPaths?.Add(nodePath);

            if (environment.FileExists(nodePath))
            {
                return new AgentLaunchSpec(nodePath, codexJsPath);
            }
        }

        return null;
    }

    private static ResolveResult Success(AgentLaunchSpec launch) =>
        new(true, launch, null);

    private static IEnumerable<string> GetKnownInstallDirectories(IExecutableEnvironment environment)
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

        yield return Path.Combine(localAppData, "Programs", "OpenAI", "Codex", "bin");
        yield return Path.Combine(localAppData, "OpenAI", "Codex", "bin");
    }

    private static IEnumerable<string> GetNpmCandidateDirectories(
        CodexAgentOptions options,
        IExecutableEnvironment environment)
    {
        var directories = new List<string>();

        void Add(string? directory)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            string expanded = environment.ExpandEnvironmentVariables(directory.Trim());
            if (directories.Exists(existing =>
                    string.Equals(existing, expanded, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            directories.Add(expanded);
        }

        if (options.AdditionalSearchPaths is { Length: > 0 })
        {
            foreach (string searchPath in options.AdditionalSearchPaths)
            {
                Add(searchPath);
            }
        }

        if (environment.IsWindows)
        {
            string? appData = environment.GetEnvironmentVariable("APPDATA");
            if (!string.IsNullOrWhiteSpace(appData))
            {
                Add(Path.Combine(appData, "npm"));
            }

            foreach (string nodeDirectory in GetNodeInstallDirectories(environment))
            {
                Add(nodeDirectory);
            }
        }

        foreach (string pathDirectory in environment.GetPathDirectories())
        {
            Add(pathDirectory);
        }

        return directories;
    }

    private static IEnumerable<string> GetNodeInstallDirectories(IExecutableEnvironment environment)
    {
        string? programFiles = environment.GetEnvironmentVariable("ProgramFiles");
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            yield return Path.Combine(programFiles, "nodejs");
        }

        string? programFilesX86 = environment.GetEnvironmentVariable("ProgramFiles(x86)");
        if (!string.IsNullOrWhiteSpace(programFilesX86))
        {
            yield return Path.Combine(programFilesX86, "nodejs");
        }
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
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            yield return Path.Combine(localAppData, "Programs", "OpenAI", "Codex", "bin", "codex.exe");
            yield return Path.Combine(localAppData, "OpenAI", "Codex", "bin", "codex.exe");
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
}
