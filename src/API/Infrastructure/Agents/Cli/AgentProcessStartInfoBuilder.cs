using System.Diagnostics;
using System.Text;

namespace Orchi.Api.Infrastructure.Agents.Cli;

internal static class AgentProcessStartInfoBuilder
{
    public static ProcessStartInfo Build(
        AgentLaunchSpec launch,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        bool useWindowsCmdShim)
    {
        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (useWindowsCmdShim && launch.UsesCmdShim)
        {
            startInfo.FileName = OperatingSystem.IsWindows() ? "cmd.exe" : launch.ExecutablePath;
            if (OperatingSystem.IsWindows())
            {
                startInfo.ArgumentList.Add("/c");
                startInfo.ArgumentList.Add(launch.ExecutablePath);
            }
        }
        else
        {
            startInfo.FileName = launch.ExecutablePath;
        }

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }
}
