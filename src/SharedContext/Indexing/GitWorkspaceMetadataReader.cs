using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace Orchi.SharedContext.Indexing;

internal static class GitWorkspaceMetadataReader
{
    public static (string? Branch, string? Head) Read(string workspacePath)
    {
        string? branch = RunGit(workspacePath, "rev-parse", "--abbrev-ref", "HEAD");
        string? head = RunGit(workspacePath, "rev-parse", "--short", "HEAD");
        return (branch, head);
    }

    private static string? RunGit(string workspacePath, params string[] args)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (string arg in args)
            {
                startInfo.ArgumentList.Add(arg);
            }

            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);
            if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(output))
            {
                return null;
            }

            return output;
        }
        catch
        {
            return null;
        }
    }
}
