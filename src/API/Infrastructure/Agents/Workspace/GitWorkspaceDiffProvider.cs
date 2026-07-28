using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace Orchi.Api.Infrastructure.Agents.Workspace;

public sealed class GitWorkspaceDiffProvider : IWorkspaceDiffProvider
{
    internal const int MaxDiffChars = 512_000;

    /// <summary>
    /// Safety cap for non-diff git output (rev-parse, ls-files, numstat). Prevents
    /// pathological repos from allocating multi-GB strings via <c>ReadToEnd</c>.
    /// </summary>
    internal const int MaxGitMetaOutputChars = 8_000_000;

    /// <summary>
    /// Skip untracked files larger than this when building review diffs. Diffing them
    /// is useless once we truncate to <see cref="MaxDiffChars"/> and risks huge peaks.
    /// </summary>
    internal const long MaxUntrackedFileBytes = 256_000;

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

        string uncommitted = RunGitDiff(workspacePath, "diff", "HEAD");
        string untracked = BuildUntrackedDiff(workspacePath);
        string combined = CombineDiffParts(uncommitted, untracked);
        if (!string.IsNullOrWhiteSpace(combined))
        {
            return FormatSection(DescribeDiffSource(uncommitted, untracked), Truncate(combined));
        }

        string lastCommit = RunGitDiff(workspacePath, "show", "HEAD", "--format=", "--patch", "--no-color");
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

        string range = $"{resolvedBase}...{resolvedHead}";
        string threeDot = RunGitDiff(workspacePath, "diff", "--no-color", range);
        if (!string.IsNullOrWhiteSpace(threeDot) && !LooksLikeGitError(threeDot))
        {
            return FormatSection($"git diff {range}", Truncate(threeDot));
        }

        string twoDotRange = $"{resolvedBase}..{resolvedHead}";
        string twoDot = RunGitDiff(workspacePath, "diff", "--no-color", twoDotRange);
        if (!string.IsNullOrWhiteSpace(twoDot) && !LooksLikeGitError(twoDot))
        {
            return FormatSection($"git diff {twoDotRange}", Truncate(twoDot));
        }

        if (!string.IsNullOrWhiteSpace(threeDot) && LooksLikeGitError(threeDot))
        {
            return $"Failed to compute branch diff for {range}: {threeDot.Trim()}";
        }

        return $"No changes detected between `{resolvedBase}` and `{resolvedHead}`.";
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
            if (builder.Length >= MaxDiffChars)
            {
                break;
            }

            if (TryDescribeOmittedUntrackedFile(workspacePath, file, out string omittedNotice))
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(omittedNotice);
                builder.Append('\n');
                continue;
            }

            int remaining = MaxDiffChars - builder.Length;
            // Exit code 1 with stdout is expected when the file differs from /dev/null.
            string fileDiff = RunGitBounded(
                workspacePath,
                remaining,
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
        }

        return builder.ToString();
    }

    internal static bool IsOrchiInternalPath(string relativePath)
    {
        string normalized = relativePath.Replace('\\', '/').Trim();
        return normalized.Equals(".orchi", StringComparison.OrdinalIgnoreCase)
               || normalized.StartsWith(".orchi/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryDescribeOmittedUntrackedFile(
        string workspacePath,
        string relativePath,
        out string notice)
    {
        notice = string.Empty;
        try
        {
            string absolutePath = Path.Combine(
                workspacePath,
                relativePath.Replace('/', Path.DirectorySeparatorChar));
            var info = new FileInfo(absolutePath);
            if (!info.Exists || info.Length <= MaxUntrackedFileBytes)
            {
                return false;
            }

            notice =
                $"diff --git a/{relativePath} b/{relativePath}\n" +
                "new file mode 100644\n" +
                $"--- /dev/null\n+++ b/{relativePath}\n" +
                $"[file omitted from review diff: {info.Length:N0} bytes exceeds {MaxUntrackedFileBytes:N0}-byte limit]";
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
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
            .Where(path => path.Length > 0 && !IsOrchiInternalPath(path))
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

    private static string RunGit(string workspacePath, params string[] args) =>
        RunGitBounded(workspacePath, MaxGitMetaOutputChars, args);

    private static string RunGitDiff(string workspacePath, params string[] args) =>
        RunGitBounded(workspacePath, MaxDiffChars, args);

    private static string RunGitBounded(string workspacePath, int maxOutputChars, params string[] args)
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

            // Drain stderr concurrently so a full stderr pipe cannot deadlock stdout reads.
            Task<string> stderrTask = process.StandardError.ReadToEndAsync();
            string stdout = ReadStreamBounded(process.StandardOutput, process, maxOutputChars);
            string stderr = stderrTask.GetAwaiter().GetResult();
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

    /// <summary>
    /// Reads at most <paramref name="maxChars"/> from git stdout. If the cap is hit, the
    /// process is killed so git cannot keep writing multi-GB output into the pipe.
    /// </summary>
    internal static string ReadStreamBounded(StreamReader reader, Process process, int maxChars)
    {
        if (maxChars <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(Math.Min(maxChars, 64_000));
        var buffer = new char[16_384];
        while (builder.Length < maxChars)
        {
            int toRead = Math.Min(buffer.Length, maxChars - builder.Length);
            int read = reader.Read(buffer, 0, toRead);
            if (read <= 0)
            {
                break;
            }

            builder.Append(buffer, 0, read);
        }

        if (builder.Length >= maxChars && !process.HasExited)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
            {
                // Best-effort: process may have exited between HasExited and Kill.
            }
        }

        return builder.ToString();
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
