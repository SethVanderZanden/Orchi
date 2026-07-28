using Orchi.Api.Infrastructure.Agents.Workspace;

namespace Orchi.Api.Tests.Infrastructure.Agents.Workspace;

public class GitWorkspaceDiffProviderTests : IDisposable
{
    private readonly string _workspacePath;
    private readonly GitWorkspaceDiffProvider _provider = new();

    public GitWorkspaceDiffProviderTests()
    {
        _workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-git-diff-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workspacePath);
    }

    public void Dispose()
    {
        if (!Directory.Exists(_workspacePath))
        {
            return;
        }

        try
        {
            Directory.Delete(_workspacePath, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [Fact]
    public void GetDiff_WhenNotGitRepository_ReturnsMessage()
    {
        string diff = _provider.GetDiff(_workspacePath);

        Assert.Contains("No git repository", diff);
    }

    [Fact]
    public void GetDiff_WhenUncommittedChanges_ReturnsGitDiffHead()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        InitializeRepoWithCommit();
        File.AppendAllText(Path.Combine(_workspacePath, "tracked.txt"), "change\n");

        string diff = _provider.GetDiff(_workspacePath);

        Assert.Contains("Change source: git diff HEAD", diff);
        Assert.Contains("tracked.txt", diff);
    }

    [Fact]
    public void GetDiff_WhenOnlyUntrackedFiles_ReturnsUntrackedDiff()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        InitializeRepoWithCommit();
        File.WriteAllText(Path.Combine(_workspacePath, "new-file.txt"), "brand new\n");

        string diff = _provider.GetDiff(_workspacePath);

        Assert.Contains("Change source: untracked files (vs HEAD)", diff);
        Assert.Contains("new-file.txt", diff);
        Assert.Contains("brand new", diff);
    }

    [Fact]
    public void GetDiff_WhenTrackedAndUntrackedChanges_IncludesBoth()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        InitializeRepoWithCommit();
        File.AppendAllText(Path.Combine(_workspacePath, "tracked.txt"), "change\n");
        File.WriteAllText(Path.Combine(_workspacePath, "new-file.txt"), "brand new\n");

        string diff = _provider.GetDiff(_workspacePath);

        Assert.Contains("Change source: git diff HEAD (including untracked files)", diff);
        Assert.Contains("tracked.txt", diff);
        Assert.Contains("new-file.txt", diff);
        Assert.Contains("brand new", diff);
    }

    [Fact]
    public void GetBranchDiff_ReturnsThreeDotDiff()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        InitializeRepoWithCommit();
        RunGit("checkout", "-b", "feature");
        File.WriteAllText(Path.Combine(_workspacePath, "feature.txt"), "feature\n");
        RunGit("add", "feature.txt");
        RunGit("-c", "user.email=test@example.com", "-c", "user.name=Test", "commit", "-m", "feature");
        RunGit("checkout", "-");

        string? baseBranch = RunGitOutput("branch", "--show-current").Trim();
        string diff = _provider.GetBranchDiff(_workspacePath, baseBranch, "feature");

        Assert.Contains("feature.txt", diff);
        Assert.Contains("...", diff);
    }

    [Fact]
    public void Truncate_AppendsNoticeWhenDiffTooLarge()
    {
        string large = new string('a', GitWorkspaceDiffProvider.MaxDiffChars + 10);

        string truncated = GitWorkspaceDiffProvider.Truncate(large);

        Assert.Contains("[diff truncated", truncated);
        Assert.True(truncated.Length <= large.Length + 128);
    }

    [Fact]
    public void ParseNumStat_ParsesAddedRemovedAndPath()
    {
        IReadOnlyList<WorkspaceDiffStatsEntry> entries = GitWorkspaceDiffProvider.ParseNumStat(
            """
            5	3	src/foo.ts
            10	0	src/bar.ts
            """);

        Assert.Equal(2, entries.Count);
        Assert.Equal("src/foo.ts", entries[0].Path);
        Assert.Equal(5, entries[0].Added);
        Assert.Equal(3, entries[0].Removed);
        Assert.Equal("src/bar.ts", entries[1].Path);
        Assert.Equal(10, entries[1].Added);
        Assert.Equal(0, entries[1].Removed);
    }

    [Fact]
    public void TryGetDiffStats_WhenUncommittedChanges_ReturnsNumStat()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        InitializeRepoWithCommit();
        File.AppendAllText(Path.Combine(_workspacePath, "tracked.txt"), "change\n");

        WorkspaceDiffStats? stats = _provider.TryGetDiffStats(_workspacePath);

        Assert.NotNull(stats);
        Assert.Contains(stats.Files, file => file.Path == "tracked.txt");
        Assert.True(stats.TotalAdded > 0);
        Assert.Equal("git diff --numstat HEAD", stats.Source);
    }

    [Fact]
    public void TryGetDiffStats_WhenOnlyUntrackedFiles_ReturnsNumStat()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        InitializeRepoWithCommit();
        File.WriteAllText(Path.Combine(_workspacePath, "new-file.txt"), "brand new\n");

        WorkspaceDiffStats? stats = _provider.TryGetDiffStats(_workspacePath);

        Assert.NotNull(stats);
        Assert.Contains(stats.Files, file => file.Path == "new-file.txt");
        Assert.True(stats.TotalAdded > 0);
        Assert.Equal(0, stats.TotalRemoved);
        Assert.Equal("untracked files (vs HEAD)", stats.Source);
    }

    [Fact]
    public void TryGetDiffStats_WhenNotGitRepository_ReturnsNull()
    {
        WorkspaceDiffStats? stats = _provider.TryGetDiffStats(_workspacePath);

        Assert.Null(stats);
    }

    private void InitializeRepoWithCommit()
    {
        RunGit("init");
        File.WriteAllText(Path.Combine(_workspacePath, "tracked.txt"), "initial\n");
        RunGit("add", "tracked.txt");
        RunGit("-c", "user.email=test@example.com", "-c", "user.name=Test", "commit", "-m", "init");
    }

    private void RunGit(params string[] args)
    {
        using var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.WorkingDirectory = _workspacePath;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        foreach (string arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();
        process.WaitForExit();
    }

    private string RunGitOutput(params string[] args)
    {
        using var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.WorkingDirectory = _workspacePath;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        foreach (string arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();
        string stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return stdout;
    }

    private static bool IsGitAvailable()
    {
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.ArgumentList.Add("--version");
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
