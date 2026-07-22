using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Orchi.Api.Infrastructure.Agents.Workspace;

public sealed class GitWorkspaceDiffProvider : IWorkspaceDiffProvider
{
    internal const int MaxDiffChars = 512_000;

    internal static string? TryGetHeadRevision(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            return null;
        }

        if (!IsGitRepository(workspacePath))
        {
            return null;
        }

        string revision = RunGit(workspacePath, "rev-parse", "HEAD").Trim();
        return string.IsNullOrWhiteSpace(revision) ? null : revision;
    }

    public string GetDiff(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            return "Workspace path is missing or does not exist.";
        }

        if (!IsGitRepository(workspacePath))
        {
            return "No git repository detected in workspace.";
        }

        string uncommitted = RunGit(workspacePath, "diff", "HEAD");
        if (!string.IsNullOrWhiteSpace(uncommitted))
        {
            return FormatSection("git diff HEAD", Truncate(uncommitted));
        }

        string lastCommit = RunGit(workspacePath, "show", "HEAD", "--format=", "--patch", "--no-color");
        if (!string.IsNullOrWhiteSpace(lastCommit))
        {
            return FormatSection("git show HEAD", Truncate(lastCommit));
        }

        return "No changes detected (git diff HEAD and git show HEAD are empty).";
    }

    public string GetBranchDiff(string workspacePath, string baseBranch, string headBranch)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            return "Workspace path is missing or does not exist.";
        }

        if (!IsGitRepository(workspacePath))
        {
            return "No git repository detected in workspace.";
        }

        string baseRef = baseBranch.Trim();
        string headRef = headBranch.Trim();
        if (string.IsNullOrWhiteSpace(baseRef) || string.IsNullOrWhiteSpace(headRef))
        {
            return "Base and head branches are required for a branch review diff.";
        }

        string range = $"{baseRef}...{headRef}";
        string threeDot = RunGit(workspacePath, "diff", "--no-color", range);
        if (!string.IsNullOrWhiteSpace(threeDot) && !LooksLikeGitError(threeDot))
        {
            return FormatSection($"git diff {range}", Truncate(threeDot));
        }

        string twoDotRange = $"{baseRef}..{headRef}";
        string twoDot = RunGit(workspacePath, "diff", "--no-color", twoDotRange);
        if (!string.IsNullOrWhiteSpace(twoDot) && !LooksLikeGitError(twoDot))
        {
            return FormatSection($"git diff {twoDotRange}", Truncate(twoDot));
        }

        if (!string.IsNullOrWhiteSpace(threeDot))
        {
            return $"Failed to compute branch diff for {range}: {threeDot.Trim()}";
        }

        return $"No changes detected between `{baseRef}` and `{headRef}`.";
    }

    private static bool LooksLikeGitError(string output)
    {
        string trimmed = output.Trim();
        return trimmed.StartsWith("fatal:", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("error:", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("unknown revision", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("bad revision", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsGitRepository(string workspacePath)
    {
        string output = RunGit(workspacePath, "rev-parse", "--is-inside-work-tree");
        return string.Equals(output.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string RunGit(string workspacePath, params string[] args)
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                WorkingDirectory = workspacePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            foreach (string arg in args)
            {
                process.StartInfo.ArgumentList.Add(arg);
            }

            process.Start();

            string stdout = process.StandardOutput.ReadToEnd();
            string stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(stdout))
            {
                return stderr.Trim();
            }

            return stdout;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return $"Failed to run git: {ex.Message}";
        }
    }

    internal static string Truncate(string diff)
    {
        if (diff.Length <= MaxDiffChars)
        {
            return diff.Trim();
        }

        return diff[..MaxDiffChars].TrimEnd() +
               $"\n\n[diff truncated at {MaxDiffChars:N0} characters]";
    }

    private static string FormatSection(string source, string diff) =>
        $"""
        Change source: {source}

        ```diff
        {diff}
        ```
        """;
}
