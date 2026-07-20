using Orchi.Api.Infrastructure.Agents.Cli;

namespace Orchi.Api.Infrastructure.Agents.Codex;

internal sealed class CodexCliInstallLayout : IAgentCliInstallLayout
{
    private static readonly AgentCliPlatformPackage[] NativePackages =
    [
        new(
            AgentCliHostPlatform.Windows,
            AgentCliHostArchitecture.X64,
            "@openai/codex-win32-x64",
            Path.Combine("vendor", "x86_64-pc-windows-msvc", "codex", "codex.exe")),
        new(
            AgentCliHostPlatform.Windows,
            AgentCliHostArchitecture.Arm64,
            "@openai/codex-win32-arm64",
            Path.Combine("vendor", "aarch64-pc-windows-msvc", "codex", "codex.exe")),
        new(
            AgentCliHostPlatform.MacOS,
            AgentCliHostArchitecture.X64,
            "@openai/codex-darwin-x64",
            Path.Combine("vendor", "x86_64-apple-darwin", "codex", "codex")),
        new(
            AgentCliHostPlatform.MacOS,
            AgentCliHostArchitecture.Arm64,
            "@openai/codex-darwin-arm64",
            Path.Combine("vendor", "aarch64-apple-darwin", "codex", "codex")),
        new(
            AgentCliHostPlatform.Linux,
            AgentCliHostArchitecture.X64,
            "@openai/codex-linux-x64",
            Path.Combine("vendor", "x86_64-unknown-linux-musl", "codex", "codex")),
        new(
            AgentCliHostPlatform.Linux,
            AgentCliHostArchitecture.Arm64,
            "@openai/codex-linux-arm64",
            Path.Combine("vendor", "aarch64-unknown-linux-musl", "codex", "codex"))
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

    public IEnumerable<string> GetFallbackPaths(
        IExecutableEnvironment environment,
        IReadOnlyList<string> candidateNames)
    {
        foreach (string directory in AgentCliKnownDirectories.For(environment))
        {
            foreach (string candidateName in candidateNames)
            {
                string baseName = Path.HasExtension(candidateName)
                    ? Path.GetFileNameWithoutExtension(candidateName)
                    : candidateName;

                if (environment.HostPlatform == AgentCliHostPlatform.Windows)
                {
                    yield return Path.Combine(directory, baseName + ".exe");
                    yield return Path.Combine(directory, "codex.exe");
                    yield return Path.Combine(directory, baseName + ".cmd");
                    yield return Path.Combine(directory, "codex.cmd");
                }
                else
                {
                    yield return Path.Combine(directory, baseName);
                    yield return Path.Combine(directory, "codex");
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

        AgentCliLaunchSpec? nativeBinary = TryResolveNativeBinary(installDirectory, environment, searchedPaths);
        if (nativeBinary is not null)
        {
            return nativeBinary;
        }

        string nodeFileName = environment.HostPlatform == AgentCliHostPlatform.Windows ? "node.exe" : "node";
        string coLocatedNode = Path.Combine(installDirectory, nodeFileName);
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
        string codexPackageRoot = Path.Combine(installDirectory, "node_modules", "@openai", "codex");
        searchedPaths?.Add(codexPackageRoot);

        foreach (AgentCliPlatformPackage package in AgentCliPlatformPackages.ForHost(NativePackages, environment))
        {
            string binName = environment.HostPlatform == AgentCliHostPlatform.Windows ? "codex.exe" : "codex";
            string[] candidatePaths =
            [
                Path.Combine(codexPackageRoot, "node_modules", package.PackageName, "bin", binName),
                Path.Combine(codexPackageRoot, "node_modules", package.PackageName, package.RelativeExecutablePath),
                Path.Combine(installDirectory, "node_modules", package.PackageName, "bin", binName),
                Path.Combine(installDirectory, "node_modules", package.PackageName, package.RelativeExecutablePath)
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
}
