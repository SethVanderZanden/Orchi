using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal sealed class CodexCliInstallLayout : IAgentCliInstallLayout
{
    private static readonly (string Package, string VendorTriple)[] WindowsPlatformPackages =
    [
        ("@openai/codex-win32-x64", "x86_64-pc-windows-msvc"),
        ("@openai/codex-win32-arm64", "aarch64-pc-windows-msvc")
    ];

    public string AgentDisplayName => "Codex";

    public string[] GetCandidateNames(string configuredExecutable)
    {
        string trimmed = configuredExecutable.Trim();
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

    public IEnumerable<string> GetPreferredInstallDirectories(IExecutableEnvironment environment)
    {
        if (!environment.IsWindows)
        {
            yield break;
        }

        string? appData = environment.GetEnvironmentVariable("APPDATA");
        if (!string.IsNullOrWhiteSpace(appData))
        {
            yield return Path.Combine(appData, "npm");
        }

        string? programFiles = environment.GetEnvironmentVariable("ProgramFiles");
        if (!string.IsNullOrWhiteSpace(programFiles))
        {
            yield return Path.Combine(programFiles, "nodejs");
        }
    }

    public IEnumerable<string> GetWindowsFallbackPaths(
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

    public AgentCliLaunchSpec? TryResolveBundle(
        string installDirectory,
        IExecutableEnvironment environment,
        ICollection<string>? searchedPaths = null)
    {
        if (!environment.DirectoryExists(installDirectory))
        {
            return null;
        }

        AgentCliLaunchSpec? nativeBinary = TryResolveNativeBinary(installDirectory, environment, searchedPaths);
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
            return new AgentCliLaunchSpec(coLocatedNode, codexJs);
        }

        return null;
    }

    private static AgentCliLaunchSpec? TryResolveNativeBinary(
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
                    return new AgentCliLaunchSpec(candidatePath, null);
                }
            }
        }

        return null;
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
}
