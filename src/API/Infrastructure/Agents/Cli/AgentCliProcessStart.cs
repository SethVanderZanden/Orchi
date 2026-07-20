using System.Diagnostics;
using System.Text;

namespace Orchi.Api.Infrastructure.Agents.Cli;

/// <summary>
/// Builds a redirected <see cref="ProcessStartInfo"/> for an agent CLI launch.
/// On Windows, npm <c>.cmd</c>/<c>.bat</c> shims cannot be started with
/// <c>UseShellExecute=false</c> (CreateProcess). Orchi wraps them with
/// <c>cmd.exe /d /c</c> so stdout/stderr stay redirectable.
/// </summary>
internal static class AgentCliProcessStart
{
    public static ProcessStartInfo Create(
        AgentCliLaunchSpec launch,
        string? workingDirectory,
        IEnumerable<string> arguments)
    {
        var argumentList = new List<string>();

        if (!string.IsNullOrWhiteSpace(launch.EntryScript))
        {
            argumentList.Add(launch.EntryScript);
        }

        argumentList.AddRange(arguments.Where(argument => !string.IsNullOrWhiteSpace(argument)));

        if (launch.RequiresWindowsShell && OperatingSystem.IsWindows())
        {
            return CreateWindowsShellStartInfo(launch.ExecutablePath, workingDirectory, argumentList);
        }

        var startInfo = CreateBaseStartInfo(launch.ExecutablePath, workingDirectory);
        foreach (string argument in argumentList)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return startInfo;
    }

    private static ProcessStartInfo CreateWindowsShellStartInfo(
        string scriptPath,
        string? workingDirectory,
        IReadOnlyList<string> arguments)
    {
        string comSpec = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
        var startInfo = CreateBaseStartInfo(comSpec, workingDirectory);
        startInfo.ArgumentList.Add("/d");
        startInfo.ArgumentList.Add("/s");
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add(BuildWindowsShellCommand(scriptPath, arguments));
        return startInfo;
    }

    internal static string BuildWindowsShellCommand(string scriptPath, IReadOnlyList<string> arguments)
    {
        var builder = new StringBuilder();
        builder.Append('"');
        builder.Append(scriptPath);
        builder.Append('"');

        foreach (string argument in arguments)
        {
            builder.Append(' ');
            builder.Append(QuoteWindowsArgument(argument));
        }

        return builder.ToString();
    }

    private static string QuoteWindowsArgument(string argument)
    {
        if (argument.Length == 0)
        {
            return "\"\"";
        }

        bool needsQuotes = argument.Any(character =>
            char.IsWhiteSpace(character) || character is '"' or '&' or '|' or '<' or '>' or '^' or '%');

        if (!needsQuotes)
        {
            return argument;
        }

        var builder = new StringBuilder();
        builder.Append('"');

        int backslashCount = 0;
        foreach (char character in argument)
        {
            if (character == '\\')
            {
                backslashCount++;
                continue;
            }

            if (character == '"')
            {
                builder.Append('\\', backslashCount * 2 + 1);
                builder.Append('"');
                backslashCount = 0;
                continue;
            }

            if (backslashCount > 0)
            {
                builder.Append('\\', backslashCount);
                backslashCount = 0;
            }

            builder.Append(character);
        }

        if (backslashCount > 0)
        {
            builder.Append('\\', backslashCount * 2);
        }

        builder.Append('"');
        return builder.ToString();
    }

    private static ProcessStartInfo CreateBaseStartInfo(string fileName, string? workingDirectory) =>
        new()
        {
            FileName = fileName,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                ? Environment.CurrentDirectory
                : workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };
}
