using Orchi.Api.Infrastructure.Agents.Cursor;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal static class CodexAgentExecutableResolver
{
    private static readonly string[] PreferredExtensions = [".exe", ".com", ".cmd", ".bat"];

    private static readonly (string Package, string VendorTriple)[] WindowsPlatformPackages =
    [
        ("@openai/codex-win32-x64", "x86_64-pc-windows-msvc"),
        ("@openai/codex-win32-arm64", "aarch64-pc-windows-msvc")
    ];

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
                string installDirectory = Path.GetDirectoryName(absolutePath) ?? absolutePath;
                CodexAgentLaunchSpec? bundle = TryResolveCodexBundle(installDirectory, environment, searchedPaths);
                if (bundle is not null)
                {
                    return Success(bundle);
                }

                return Success(new CodexAgentLaunchSpec(absolutePath, null));
            }
        }

        foreach (string installDirectory in GetInstallDirectories(options, environment))
        {
            CodexAgentLaunchSpec? bundle = TryResolveCodexBundle(installDirectory, environment, searchedPaths);
            if (bundle is not null)
            {
                return Success(bundle);
            }
        }

        string[] candidateNames = GetCandidateNames(options.Executable);
        IEnumerable<string> searchDirectories = GetSearchDirectories(options, environment);

        string? resolved = FindInDirectories(searchDirectories, candidateNames, environment, searchedPaths);
        if (resolved is not null)
        {
            string installDirectory = Path.GetDirectoryName(resolved) ?? resolved;
            CodexAgentLaunchSpec? bundle = TryResolveCodexBundle(installDirectory, environment, searchedPaths);
            if (bundle is not null)
            {
                return Success(bundle);
            }

            return Success(new CodexAgentLaunchSpec(resolved, null));
        }

        foreach (string fallbackPath in GetWindowsFallbackPaths(environment, candidateNames))
        {
            searchedPaths.Add(fallbackPath);

            if (!environment.FileExists(fallbackPath))
            {
                continue;
            }

            string installDirectory = Path.GetDirectoryName(fallbackPath) ?? fallbackPath;
            CodexAgentLaunchSpec? bundle = TryResolveCodexBundle(installDirectory, environment, searchedPaths);
            if (bundle is not null)
            {
                return Success(bundle);
            }

            return Success(new CodexAgentLaunchSpec(fallbackPath, null));
        }

        string message =
            $"Unable to locate Codex CLI executable '{options.Executable}'. " +
            "Ensure Codex is installed and on PATH, restart the Orchi API after installing, " +
            "or set Agents:Codex:Executable to the full path. " +
            $"Searched: {string.Join("; ", searchedPaths.Distinct(StringComparer.OrdinalIgnoreCase))}";

        return new ResolveResult(false, null, message);
    }

    internal static CodexAgentLaunchSpec? TryResolveCodexBundle(
        string installDirectory,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths = null)
    {
        if (!environment.DirectoryExists(installDirectory))
        {
            return null;
        }

        CodexAgentLaunchSpec? nativeBinary = TryResolveNativeBinary(installDirectory, environment, searchedPaths);
        if (nativeBinary is not null)
        {
            return nativeBinary;
        }

        string coLocatedNode = Path.Combine(installDirectory, "node.exe");
        string codexJs = Path.Combine(installDirectory, "node_modules", "@openai", "codex", "bin", "codex.js");
        searchedPaths?.Add(coLocatedNode);
        searchedPaths?.Add(codexJs);

        if (environment.FileExists(coLocatedNode) && environment.FileExists(codexJs))
        {
            return new CodexAgentLaunchSpec(coLocatedNode, codexJs);
        }

        return null;
    }

    private static CodexAgentLaunchSpec? TryResolveNativeBinary(
        string installDirectory,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths)
    {
        if (!environment.IsWindows)
        {
            return null;
        }

        string codexPackageRoot = Path.Combine(installDirectory, "node_modules", "@openai", "codex");
        searchedPaths?.Add(codexPackageRoot);

        foreach ((string package, string vendorTriple) in WindowsPlatformPackages)
        {
            string[] candidatePaths =
            [
                Path.Combine(codexPackageRoot, "node_modules", package, "bin", "codex.exe"),
                Path.Combine(codexPackageRoot, "node_modules", package, "vendor", vendorTriple, "codex", "codex.exe"),
                Path.Combine(installDirectory, "node_modules", package, "bin", "codex.exe"),
                Path.Combine(installDirectory, "node_modules", package, "vendor", vendorTriple, "codex", "codex.exe")
            ];

            foreach (string candidatePath in candidatePaths)
            {
                searchedPaths?.Add(candidatePath);

                if (environment.FileExists(candidatePath))
                {
                    return new CodexAgentLaunchSpec(candidatePath, null);
                }
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
        string baseName = Path.GetFileNameWithoutExtension(fileName);

        return string.Equals(baseName, "codex", StringComparison.OrdinalIgnoreCase)
            ? ["codex"]
            : [fileName, baseName];
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
            foreach (string candidatePath in GetCandidatePathsForDirectory(Path.Combine(appData, "npm"), candidateNames))
            {
                yield return candidatePath;
            }
        }

        string? programFiles = environment.GetEnvironmentVariable("ProgramFiles");
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            foreach (string candidatePath in GetCandidatePathsForDirectory(Path.Combine(programFiles, "nodejs"), candidateNames))
            {
                yield return candidatePath;
            }
        }
    }

    private static IEnumerable<string> GetCandidatePathsForDirectory(
        string directory,
        IReadOnlyList<string> candidateNames)
    {
        foreach (string candidateName in candidateNames)
        {
            string baseName = Path.HasExtension(candidateName)
                ? Path.GetFileNameWithoutExtension(candidateName)
                : candidateName;

            yield return Path.Combine(directory, baseName + ".exe");
            yield return Path.Combine(directory, "codex.exe");
            yield return Path.Combine(directory, baseName + ".cmd");
            yield return Path.Combine(directory, "codex.cmd");
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
