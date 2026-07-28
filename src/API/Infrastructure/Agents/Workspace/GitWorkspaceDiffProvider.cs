using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Orchi.Api.Infrastructure.Agents.Workspace;

public sealed class GitWorkspaceDiffProvider : IWorkspaceDiffProvider
{
    internal const int MaxDiffChars = 512_000;

    /// <summary>
    /// Git accepts <c>/dev/null</c> as the empty-tree side of <c>git diff --no-index</c>
    /// on both Unix and Windows (Git for Windows).
    /// </summary>
    private const string EmptyBlobPath = "/dev/null";

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
        string untracked = BuildUntrackedDiff(workspacePath);
        string combined = CombineDiffParts(uncommitted, untracked);
        if (!string.IsNullOrWhiteSpace(combined))
        {
            return FormatSection(DescribeDiffSource(uncommitted, untracked), Truncate(combined));
        }

        string lastCommit = RunGit(workspacePath, "show", "HEAD", "--format=", "--patch", "--no-color");
        if (!string.IsNullOrWhiteSpace(lastCommit))
        {
            return FormatSection("git show HEAD", Truncate(lastCommit));
        }

        return "No changes detected (git diff HEAD, untracked files, and git show HEAD are empty).";
    }

    internal static string? TryResolveBranchRef(string workspacePath, string branchName)
    {
        string trimmed = branchName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        if (GitRefExists(workspacePath, $"refs/heads/{trimmed}"))
        {
            return trimmed;
        }

        if (GitRefExists(workspacePath, trimmed))
        {
            return trimmed;
        }

        if (GitRefExists(workspacePath, $"refs/remotes/{trimmed}"))
        {
            return trimmed;
        }

        if (GitRefExists(workspacePath, $"refs/remotes/origin/{trimmed}"))
        {
            return $"origin/{trimmed}";
        }

        return null;
    }

    public WorkspaceDiffStats? TryGetDiffStats(string workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath) || !Directory.Exists(workspacePath))
        {
            return null;
        }

        if (!IsGitRepository(workspacePath))
        {
            return null;
        }

        IReadOnlyList<WorkspaceDiffStatsEntry> uncommitted = ParseNumStat(
            RunGit(workspacePath, "diff", "--numstat", "HEAD"));
        IReadOnlyList<WorkspaceDiffStatsEntry> untracked = BuildUntrackedDiffStats(workspacePath);
        IReadOnlyList<WorkspaceDiffStatsEntry> combined = CombineStatsEntries(uncommitted, untracked);
        if (combined.Count > 0)
        {
            return BuildStats(combined, DescribeDiffStatsSource(uncommitted, untracked));
        }

        IReadOnlyList<WorkspaceDiffStatsEntry> lastCommit = ParseNumStat(
            RunGit(workspacePath, "show", "HEAD", "--numstat", "--format="));
        if (lastCommit.Count > 0)
        {
            return BuildStats(lastCommit, "git show HEAD --numstat");
        }

        return null;
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

        string? resolvedBase = TryResolveBranchRef(workspacePath, baseRef);
        if (resolvedBase is null)
        {
            return $"Failed to resolve base branch ref: `{baseRef}`.";
        }

        string? resolvedHead = TryResolveBranchRef(workspacePath, headRef);
        if (resolvedHead is null)
        {
            return $"Failed to resolve head branch ref: `{headRef}`.";
        }

        string committedDiff = TryGetCommittedBranchDiff(workspacePath, resolvedBase, resolvedHead, out string committedSource, out string? committedError);
        if (committedError is not null)
        {
            return committedError;
        }

        string uncommitted = RunGit(workspacePath, "diff", "HEAD");
        string untracked = BuildUntrackedDiff(workspacePath);
        string workingTreeDiff = CombineDiffParts(uncommitted, untracked);

        bool hasCommitted = !string.IsNullOrWhiteSpace(committedDiff);
        bool hasWorkingTree = !string.IsNullOrWhiteSpace(workingTreeDiff);

        if (hasCommitted && hasWorkingTree)
        {
            return FormatBranchDiffSections(
                resolvedBase,
                resolvedHead,
                [
                    (committedSource, Truncate(committedDiff)),
                    (DescribeDiffSource(uncommitted, untracked) + " (uncommitted in workspace)", Truncate(workingTreeDiff)),
                ]);
        }

        if (hasCommitted)
        {
            return FormatSection(committedSource, Truncate(committedDiff));
        }

        if (hasWorkingTree)
        {
            return FormatBranchDiffSections(
                resolvedBase,
                resolvedHead,
                [
                    ($"No committed changes between `{resolvedBase}` and `{resolvedHead}`; " +
                     DescribeDiffSource(uncommitted, untracked) + " (uncommitted in workspace)",
                     Truncate(workingTreeDiff)),
                ]);
        }

        return $"No changes detected between `{resolvedBase}` and `{resolvedHead}`.";
    }

    private static string TryGetCommittedBranchDiff(
        string workspacePath,
        string baseRef,
        string headRef,
        out string committedSource,
        out string? error)
    {
        string range = $"{baseRef}...{headRef}";
        string threeDot = RunGit(workspacePath, "diff", "--no-color", range);
        if (!string.IsNullOrWhiteSpace(threeDot) && !LooksLikeGitError(threeDot))
        {
            committedSource = $"git diff {range}";
            error = null;
            return threeDot;
        }

        string twoDotRange = $"{baseRef}..{headRef}";
        string twoDot = RunGit(workspacePath, "diff", "--no-color", twoDotRange);
        if (!string.IsNullOrWhiteSpace(twoDot) && !LooksLikeGitError(twoDot))
        {
            committedSource = $"git diff {twoDotRange}";
            error = null;
            return twoDot;
        }

        if (!string.IsNullOrWhiteSpace(threeDot) && LooksLikeGitError(threeDot))
        {
            committedSource = string.Empty;
            error = $"Failed to compute branch diff for {range}: {threeDot.Trim()}";
            return string.Empty;
        }

        committedSource = string.Empty;
        error = null;
        return string.Empty;
    }

    private static string FormatBranchDiffSections(
        string baseRef,
        string headRef,
        IReadOnlyList<(string Source, string Diff)> sections)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Branch review (`{baseRef}`...`{headRef}`):");
        builder.AppendLine();

        for (int index = 0; index < sections.Count; index++)
        {
            (string source, string diff) = sections[index];
            if (index > 0)
            {
                builder.AppendLine();
            }

            builder.AppendLine($"Change source: {source}");
            builder.AppendLine();
            builder.AppendLine("```diff");
            builder.AppendLine(diff);
            builder.AppendLine("```");
        }

        return builder.ToString().TrimEnd();
    }

    private static bool LooksLikeGitError(string output)
    {
        string trimmed = output.Trim();
        return trimmed.StartsWith("fatal:", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("error:", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("unknown revision", StringComparison.OrdinalIgnoreCase) ||
               trimmed.Contains("bad revision", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildUntrackedDiff(string workspacePath)
    {
        IReadOnlyList<string> files = ListUntrackedFiles(workspacePath);
        if (files.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (string file in files)
        {
            // Exit code 1 with stdout is expected when the file differs from /dev/null.
            string fileDiff = RunGit(
                workspacePath,
                "diff",
                "--no-color",
                "--no-index",
                "--",
                EmptyBlobPath,
                file);

            if (string.IsNullOrWhiteSpace(fileDiff) || LooksLikeGitError(fileDiff))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(fileDiff.TrimEnd());
            builder.Append('\n');

            if (builder.Length >= MaxDiffChars)
            {
                break;
            }
        }

        return builder.ToString();
    }

    private static IReadOnlyList<WorkspaceDiffStatsEntry> BuildUntrackedDiffStats(string workspacePath)
    {
        IReadOnlyList<string> files = ListUntrackedFiles(workspacePath);
        if (files.Count == 0)
        {
            return [];
        }

        var entries = new List<WorkspaceDiffStatsEntry>(files.Count);
        foreach (string file in files)
        {
            string numstat = RunGit(
                workspacePath,
                "diff",
                "--numstat",
                "--no-index",
                "--",
                EmptyBlobPath,
                file);

            IReadOnlyList<WorkspaceDiffStatsEntry> parsed = ParseNumStat(numstat);
            if (parsed.Count == 0)
            {
                // Still surface empty or oddly reported new files in the stats table.
                entries.Add(new WorkspaceDiffStatsEntry(file, 0, 0));
                continue;
            }

            // --no-index reports paths as "/dev/null => file"; keep the workspace-relative path.
            WorkspaceDiffStatsEntry first = parsed[0];
            entries.Add(new WorkspaceDiffStatsEntry(file, first.Added, first.Removed));
        }

        return entries;
    }

    private static IReadOnlyList<string> ListUntrackedFiles(string workspacePath)
    {
        // -z: NUL-delimited, unquoted paths (handles spaces); --exclude-standard honors .gitignore.
        string output = RunGit(workspacePath, "ls-files", "-z", "--others", "--exclude-standard");
        if (string.IsNullOrEmpty(output) || LooksLikeGitError(output))
        {
            return [];
        }

        return output
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(path => path.Trim())
            .Where(path => path.Length > 0)
            .ToArray();
    }

    private static string CombineDiffParts(string tracked, string untracked)
    {
        bool hasTracked = !string.IsNullOrWhiteSpace(tracked);
        bool hasUntracked = !string.IsNullOrWhiteSpace(untracked);
        if (!hasTracked)
        {
            return hasUntracked ? untracked.TrimEnd() + "\n" : string.Empty;
        }

        if (!hasUntracked)
        {
            return tracked;
        }

        return tracked.TrimEnd() + "\n" + untracked.TrimEnd() + "\n";
    }

    private static IReadOnlyList<WorkspaceDiffStatsEntry> CombineStatsEntries(
        IReadOnlyList<WorkspaceDiffStatsEntry> tracked,
        IReadOnlyList<WorkspaceDiffStatsEntry> untracked)
    {
        if (tracked.Count == 0)
        {
            return untracked;
        }

        if (untracked.Count == 0)
        {
            return tracked;
        }

        var combined = new List<WorkspaceDiffStatsEntry>(tracked.Count + untracked.Count);
        combined.AddRange(tracked);
        combined.AddRange(untracked);
        return combined;
    }

    private static string DescribeDiffSource(string tracked, string untracked)
    {
        bool hasTracked = !string.IsNullOrWhiteSpace(tracked);
        bool hasUntracked = !string.IsNullOrWhiteSpace(untracked);
        if (hasTracked && hasUntracked)
        {
            return "git diff HEAD (+ untracked)";
        }

        if (hasUntracked)
        {
            return "untracked files (git diff --no-index)";
        }

        return "git diff HEAD";
    }

    private static string DescribeDiffStatsSource(
        IReadOnlyList<WorkspaceDiffStatsEntry> tracked,
        IReadOnlyList<WorkspaceDiffStatsEntry> untracked)
    {
        bool hasTracked = tracked.Count > 0;
        bool hasUntracked = untracked.Count > 0;
        if (hasTracked && hasUntracked)
        {
            return "git diff --numstat HEAD (+ untracked)";
        }

        if (hasUntracked)
        {
            return "untracked files (git diff --numstat --no-index)";
        }

        return "git diff --numstat HEAD";
    }

    private static bool IsGitRepository(string workspacePath)
    {
        string output = RunGit(workspacePath, "rev-parse", "--is-inside-work-tree");
        return string.Equals(output.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool GitRefExists(string workspacePath, string refSpec)
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
            };
            process.StartInfo.ArgumentList.Add("rev-parse");
            process.StartInfo.ArgumentList.Add("--verify");
            process.StartInfo.ArgumentList.Add("--quiet");
            process.StartInfo.ArgumentList.Add(refSpec);

            process.Start();
            process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit();

            return process.ExitCode == 0;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return false;
        }
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

    internal static IReadOnlyList<WorkspaceDiffStatsEntry> ParseNumStat(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return [];
        }

        var entries = new List<WorkspaceDiffStatsEntry>();
        foreach (string rawLine in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (LooksLikeGitError(rawLine))
            {
                continue;
            }

            int firstTab = rawLine.IndexOf('\t');
            if (firstTab < 0)
            {
                continue;
            }

            int secondTab = rawLine.IndexOf('\t', firstTab + 1);
            if (secondTab < 0)
            {
                continue;
            }

            string addedToken = rawLine[..firstTab];
            string removedToken = rawLine[(firstTab + 1)..secondTab];
            string path = rawLine[(secondTab + 1)..];

            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            entries.Add(new WorkspaceDiffStatsEntry(
                path,
                ParseNumStatCount(addedToken),
                ParseNumStatCount(removedToken)));
        }

        return entries;
    }

    private static int ParseNumStatCount(string token) =>
        int.TryParse(token, out int count) ? count : 0;

    private static WorkspaceDiffStats BuildStats(
        IReadOnlyList<WorkspaceDiffStatsEntry> files,
        string source)
    {
        int totalAdded = files.Sum(file => file.Added);
        int totalRemoved = files.Sum(file => file.Removed);
        return new WorkspaceDiffStats(files, totalAdded, totalRemoved, source);
    }
}
