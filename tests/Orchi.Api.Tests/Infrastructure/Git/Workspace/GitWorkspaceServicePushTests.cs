using System.Diagnostics;
using Orchi.Api.Infrastructure.Cli;
using Orchi.Api.Infrastructure.Git.Workspace;

namespace Orchi.Api.Tests.Infrastructure.Git.Workspace;

public class GitWorkspaceServicePushTests : IDisposable
{
    private readonly string _root;
    private readonly string _remotePath;
    private readonly string _repoPath;
    private readonly GitWorkspaceService _git;

    public GitWorkspaceServicePushTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"orchi-push-{Guid.NewGuid():N}");
        _remotePath = Path.Combine(_root, "remote.git");
        _repoPath = Path.Combine(_root, "repo");
        Directory.CreateDirectory(_root);
        _git = new GitWorkspaceService(new ProcessRunner());
    }

    public void Dispose()
    {
        TryDelete(_root);
    }

    [Fact]
    public async Task CreateWorktreeAsync_FromOriginBase_DoesNotTrackBaseUpstream()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        await InitializeRemoteRepoWithStagingAsync();

        // Only origin/staging exists locally as a remote-tracking ref (common after fetch).
        RunGit(_repoPath, "checkout", "--detach");
        RunGit(_repoPath, "branch", "-D", "staging");

        GitWorktreeCreateResult created = await _git.CreateWorktreeAsync(
            _repoPath,
            planId: "20260728-c2a72b5a",
            baseBranch: "staging",
            branchName: "orchi/20260728-c2a72b5a",
            CancellationToken.None);

        Assert.Equal("orchi/20260728-c2a72b5a", created.Branch);
        Assert.Equal("staging", created.BaseBranch);

        string status = RunGitCapture(created.Path, "status", "-sb").Trim();
        Assert.StartsWith("## orchi/20260728-c2a72b5a", status, StringComparison.Ordinal);
        Assert.DoesNotContain("origin/staging", status, StringComparison.Ordinal);

        ProcessRunResult upstream = await new ProcessRunner().RunAsync(
            "git",
            ["rev-parse", "--abbrev-ref", "@{u}"],
            created.Path,
            CancellationToken.None);
        Assert.False(upstream.Succeeded);
    }

    [Fact]
    public async Task PushAsync_PushesOrchiBranchNotInheritedStagingUpstream()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        await InitializeRemoteRepoWithStagingAsync();

        GitWorktreeCreateResult created = await _git.CreateWorktreeAsync(
            _repoPath,
            planId: "push-dest",
            baseBranch: "staging",
            branchName: "orchi/push-dest",
            CancellationToken.None);

        // Simulate the bad inherited upstream that caused `git push origin orchi/x:staging`.
        RunGit(created.Path, "branch", "--set-upstream-to=origin/staging");

        File.WriteAllText(Path.Combine(created.Path, "feature.txt"), "work\n");
        await _git.CommitAsync(created.Path, "orchi work", CancellationToken.None);

        await _git.PushAsync(created.Path, setUpstream: true, CancellationToken.None);

        string remoteBranches = RunGitCapture(_repoPath, "ls-remote", "--heads", "origin");
        Assert.Contains("refs/heads/orchi/push-dest", remoteBranches, StringComparison.Ordinal);

        // Staging tip must still be the initial commit (not the orchi work commit).
        string stagingTip = RunGitCapture(_repoPath, "rev-parse", "origin/staging").Trim();
        string orchiTip = RunGitCapture(_repoPath, "rev-parse", "origin/orchi/push-dest").Trim();
        Assert.NotEqual(stagingTip, orchiTip);

        string upstream = RunGitCapture(created.Path, "rev-parse", "--abbrev-ref", "@{u}").Trim();
        Assert.Equal("origin/orchi/push-dest", upstream);
    }

    private async Task InitializeRemoteRepoWithStagingAsync()
    {
        RunGit(_root, "init", "--bare", _remotePath);
        RunGit(_root, "clone", _remotePath, _repoPath);
        RunGit(_repoPath, "checkout", "-b", "staging");
        File.WriteAllText(Path.Combine(_repoPath, "readme.txt"), "base\n");
        RunGit(_repoPath, "add", "readme.txt");
        RunGit(_repoPath, "-c", "user.email=test@example.com", "-c", "user.name=Test", "commit", "-m", "init");
        RunGit(_repoPath, "push", "-u", "origin", "staging");

        Assert.True(await _git.IsGitRepositoryAsync(_repoPath, CancellationToken.None));
    }

    private static void RunGit(string workingDirectory, params string[] args)
    {
        ProcessRunResult result = RunGitResult(workingDirectory, args);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {result.CombinedOutput}");
        }
    }

    private static string RunGitCapture(string workingDirectory, params string[] args)
    {
        ProcessRunResult result = RunGitResult(workingDirectory, args);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {result.CombinedOutput}");
        }

        return result.StdOut;
    }

    private static ProcessRunResult RunGitResult(string workingDirectory, params string[] args)
    {
        using var process = new Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        foreach (string arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();
        string stdOut = process.StandardOutput.ReadToEnd();
        string stdErr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return new ProcessRunResult(process.ExitCode, stdOut, stdErr);
    }

    private static bool IsGitAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit(5000);
            return process is { ExitCode: 0 };
        }
        catch
        {
            return false;
        }
    }

    private static void TryDelete(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
