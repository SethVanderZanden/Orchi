using Orchi.Api.Infrastructure.Agents.Cursor;

namespace Orchi.Api.Infrastructure.Agents.Codex;

/// <summary>
/// Resolves the Codex CLI the same way a terminal does: prefer the <c>codex</c>
/// shortcut on PATH (<c>codex.exe</c> / <c>codex.cmd</c>), then known installer
/// locations. Does not dig into nested npm platform-package vendor paths —
/// those are long, fragile, and break Windows MAX_PATH when the workspace is deep.
/// </summary>
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
                return Success(new CodexAgentLaunchSpec(absolutePath, null));
            }
        }

        string[] candidateNames = GetCandidateNames(options.Executable);

        // Same as typing `codex` in a shell: search PATH (+ optional extra dirs).
        string? resolved = FindInDirectories(
            GetSearchDirectories(options, environment),
            candidateNames,
            environment,
            searchedPaths);
        if (resolved is not null)
        {
            return Success(new CodexAgentLaunchSpec(resolved, null));
        }

        // Known Windows installers that may not yet be on the API process PATH.
        foreach (string installDirectory in GetKnownInstallDirectories(environment))
        {
            string coLocatedExe = Path.Combine(installDirectory, "codex.exe");
            searchedPaths.Add(coLocatedExe);

            if (environment.FileExists(coLocatedExe))
            {
                return Success(new CodexAgentLaunchSpec(coLocatedExe, null));
            }
        }

        foreach (string fallbackPath in GetWindowsFallbackPaths(environment, candidateNames))
        {
            searchedPaths.Add(fallbackPath);

            if (environment.FileExists(fallbackPath))
            {
                return Success(new CodexAgentLaunchSpec(fallbackPath, null));
            }
        }

        // Last resort: npm node.exe + codex.js (only when no .exe/.cmd was found).
        foreach (string installDirectory in GetNpmCandidateDirectories(options, environment))
        {
            CodexAgentLaunchSpec? nodeBundle = TryResolveNpmNodeBundle(installDirectory, environment, searchedPaths);
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

        // Common Windows layout: node.exe in Program Files\nodejs, global packages in %APPDATA%\npm.
        return TryResolveSplitNpmNodeBundle(installDirectory, environment, searchedPaths);
    }

    private static CodexAgentLaunchSpec? TryResolveSplitNpmNodeBundle(
        string installDirectory,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths)
    {
        if (!environment.IsWindows)
        {
            return null;
        }

        // installDirectory is often %APPDATA%\npm (has package, no node.exe).
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
                return new CodexAgentLaunchSpec(nodePath, codexJsPath);
            }
        }

        return null;
    }

    private static ResolveResult Success(CodexAgentLaunchSpec launch) =>
        new(true, launch, null);

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

        // PowerShell / standalone installer (https://developers.openai.com/codex/cli)
        yield return Path.Combine(localAppData, "Programs", "OpenAI", "Codex", "bin");
        // Codex desktop app CLI
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
