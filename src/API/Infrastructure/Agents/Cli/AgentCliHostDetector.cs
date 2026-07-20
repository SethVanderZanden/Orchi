using System.Runtime.InteropServices;

namespace Orchi.Api.Infrastructure.Agents.Cli;

internal static class AgentCliHostDetector
{
    public static AgentCliHostPlatform DetectPlatform()
    {
        if (OperatingSystem.IsWindows())
        {
            return AgentCliHostPlatform.Windows;
        }

        if (OperatingSystem.IsMacOS())
        {
            return AgentCliHostPlatform.MacOS;
        }

        if (OperatingSystem.IsLinux())
        {
            return AgentCliHostPlatform.Linux;
        }

        return AgentCliHostPlatform.Unknown;
    }

    public static AgentCliHostArchitecture DetectArchitecture()
    {
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => AgentCliHostArchitecture.X64,
            Architecture.Arm64 => AgentCliHostArchitecture.Arm64,
            _ => AgentCliHostArchitecture.Unknown
        };
    }

    public static AgentCliInstallKind DetectInstallKind(string resolvedPath, AgentCliHostPlatform platform)
    {
        if (string.IsNullOrWhiteSpace(resolvedPath))
        {
            return AgentCliInstallKind.Unknown;
        }

        string normalized = resolvedPath.Replace('\\', '/');

        if (ContainsOrdinalIgnoreCase(normalized, "/node_modules/") ||
            ContainsOrdinalIgnoreCase(normalized, "/npm/") ||
            EndsWithOrdinalIgnoreCase(normalized, "/npm") ||
            ContainsOrdinalIgnoreCase(normalized, "/.npm-global/"))
        {
            return AgentCliInstallKind.NpmGlobal;
        }

        if (ContainsOrdinalIgnoreCase(normalized, "/volta/") ||
            ContainsOrdinalIgnoreCase(normalized, "/.volta/"))
        {
            return AgentCliInstallKind.Volta;
        }

        if (ContainsOrdinalIgnoreCase(normalized, "/pnpm/") ||
            ContainsOrdinalIgnoreCase(normalized, "/.local/share/pnpm/"))
        {
            return AgentCliInstallKind.Pnpm;
        }

        if (platform is AgentCliHostPlatform.MacOS or AgentCliHostPlatform.Linux &&
            (ContainsOrdinalIgnoreCase(normalized, "/opt/homebrew/") ||
             ContainsOrdinalIgnoreCase(normalized, "/homebrew/") ||
             ContainsOrdinalIgnoreCase(normalized, "/usr/local/cellar/") ||
             ContainsOrdinalIgnoreCase(normalized, "/home/linuxbrew/")))
        {
            return AgentCliInstallKind.Homebrew;
        }

        if (ContainsOrdinalIgnoreCase(normalized, "/cursor-agent/") ||
            ContainsOrdinalIgnoreCase(normalized, "/.local/share/cursor-agent/") ||
            ContainsOrdinalIgnoreCase(normalized, "/vendor/") && ContainsOrdinalIgnoreCase(normalized, "/codex"))
        {
            return AgentCliInstallKind.NativeInstaller;
        }

        return AgentCliInstallKind.Unknown;
    }

    private static bool ContainsOrdinalIgnoreCase(string haystack, string needle) =>
        haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);

    private static bool EndsWithOrdinalIgnoreCase(string value, string suffix) =>
        value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
}
