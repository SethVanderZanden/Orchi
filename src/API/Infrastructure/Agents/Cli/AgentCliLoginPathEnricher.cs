using System.Diagnostics;
using System.Text;

namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Soft-fail PATH enrichment for GUI-hosted processes (Finder/Dock/Visual Studio) that do not
/// inherit the user's login-shell PATH. Probes login shell and, on macOS, launchctl.
/// Never throws — enrichment is best-effort and skipped when probes fail or time out.
/// </summary>
internal static class AgentCliLoginPathEnricher
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

    private static readonly Lazy<IReadOnlyList<string>> ExtraDirectories = new(ProbeExtraDirectories);

    public static IReadOnlyList<string> GetExtraPathDirectories(IExecutableEnvironment environment)
    {
        if (environment.HostPlatform is not (AgentCliHostPlatform.MacOS or AgentCliHostPlatform.Linux))
        {
            return [];
        }

        // Tests / fakes: only use the lazy probe against the real host when using Current.
        if (!ReferenceEquals(environment, ExecutableEnvironment.Current))
        {
            return [];
        }

        return ExtraDirectories.Value;
    }

    internal static IReadOnlyList<string> ParsePathDirectories(string? pathValue, char separator)
    {
        if (string.IsNullOrWhiteSpace(pathValue))
        {
            return [];
        }

        return pathValue
            .Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.Trim('"'))
            .Where(entry => entry.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static IReadOnlyList<string> ProbeExtraDirectories()
    {
        var directories = new List<string>();

        try
        {
            if (OperatingSystem.IsMacOS())
            {
                AddRange(directories, ParsePathDirectories(TryReadLaunchctlPath(), ':'));
            }

            AddRange(directories, ParsePathDirectories(TryReadLoginShellPath(), ':'));
        }
        catch
        {
            // Soft-fail: never block CLI resolution because a shell probe failed.
        }

        return directories;
    }

    private static void AddRange(List<string> directories, IEnumerable<string> source)
    {
        foreach (string directory in source)
        {
            if (!directories.Contains(directory, StringComparer.Ordinal))
            {
                directories.Add(directory);
            }
        }
    }

    private static string? TryReadLaunchctlPath()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return null;
        }

        return TryRunCapture("/bin/launchctl", ["getenv", "PATH"]);
    }

    private static string? TryReadLoginShellPath()
    {
        string shell = Environment.GetEnvironmentVariable("SHELL")
            ?? (OperatingSystem.IsMacOS() ? "/bin/zsh" : "/bin/bash");

        if (!File.Exists(shell))
        {
            return null;
        }

        // Login shell so ~/.zprofile / ~/.bash_profile PATH entries appear for GUI hosts.
        string? output = TryRunCapture(shell, ["-l", "-c", "printf '%s' \"$PATH\""]);
        return string.IsNullOrWhiteSpace(output) ? null : output.Trim();
    }

    private static string? TryRunCapture(string fileName, IReadOnlyList<string> arguments)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8
            };

            foreach (string argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            if (!process.Start())
            {
                return null;
            }

            Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
            if (!process.WaitForExit((int)ProbeTimeout.TotalMilliseconds))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // ignore
                }

                return null;
            }

            string stdout = stdoutTask.GetAwaiter().GetResult();
            return process.ExitCode == 0 ? stdout : null;
        }
        catch
        {
            return null;
        }
    }
}
