using Orchi.Api.Infrastructure.Cli;
using Orchi.Api.Infrastructure.Git.Workspace;

namespace Orchi.Api.Tests.Infrastructure.Git.Workspace;

public sealed class GitWorkspaceServicePushTests : IDisposable
{
    private readonly string _repoPath;
    private readonly string _bareRemotePath;
    private readonly GitWorkspaceService _gitWorkspaceService;

    public GitWorkspaceServicePushTests()
    {
        string root = Path.Combine(Path.GetTempPath(), $"orchi-git-push-{Guid.NewGuid():N}");
        _repoPath = Path.Combine(root, "repo");
        _bareRemotePath = Path.Combine(root, "bare");
        Directory.CreateDirectory(_repoPath);
        Directory.CreateDirectory(_bareRemotePath);
        _gitWorkspaceService = new GitWorkspaceService(new ProcessRunner());
    }

    public void Dispose()
    {
        string root = Directory.GetParent(_repoPath)?.FullName;
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root))
        {
            return;
        }

        try
        {
            Directory.Delete(root, recursive: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    [Fact]
    public async Task CreateWorktreeAsync_FromRemoteTrackingBase_DoesNotInheritUpstream()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        await InitializeRemoteRepositoryAsync();

        GitWorktreeCreateResult created = await _gitWorkspaceService.CreateWorktreeAsync(
            _repoPath,
            planId: "test-worktree",
            baseBranch: "origin/staging",
            branchName: "orchi/test-worktree",
            CancellationToken.None);

        string? remote = TryGetGitConfig(_repoPath, $"branch.{created.Branch}.remote");
        string? merge = TryGetGitConfig(_repoPath, $"branch.{created.Branch}.merge");

        Assert.Null(remote);
        Assert.Null(merge);
    }

    [Fact]
    public async Task PushAsync_WithInheritedUpstream_PushesToSameNamedRemoteBranch()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        await InitializeRemoteRepositoryAsync();

        GitWorktreeCreateResult created = await _gitWorkspaceService.CreateWorktreeAsync(
            _repoPath,
            planId: "push-test",
            baseBranch: "origin/staging",
            branchName: "orchi/push-test",
            CancellationToken.None);

        // Simulate older worktrees that inherited the base branch upstream.
        RunGitInRepo("config", $"branch.{created.Branch}.remote", "origin");
        RunGitInRepo("config", $"branch.{created.Branch}.merge", "refs/heads/staging");
        RunGitInWorktree(created.Path, "config", "push.default", "upstream");

        File.AppendAllText(Path.Combine(created.Path, "change.txt"), "change\n");
        await _gitWorkspaceService.CommitAsync(created.Path, "test change", CancellationToken.None);
        await _gitWorkspaceService.PushAsync(created.Path, setUpstream: false, CancellationToken.None);

        string remoteBranches = RunGitInRepo("ls-remote", "--heads", "origin");
        Assert.Contains("refs/heads/orchi/push-test", remoteBranches, StringComparison.Ordinal);

        string pushedTip = RunGitInRepo("rev-parse", "refs/remotes/origin/orchi/push-test");
        string stagingTip = RunGitInRepo("rev-parse", "refs/remotes/origin/staging");
        Assert.NotEqual(pushedTip, stagingTip);
    }

    private async Task InitializeRemoteRepositoryAsync()
    {
        RunGitInRepo("init", "-b", "main");
        File.WriteAllText(Path.Combine(_repoPath, "readme.txt"), "base\n");
        RunGitInRepo("add", "readme.txt");
        RunGitInRepo("-c", "user.email=test@example.com", "-c", "user.name=Test", "commit", "-m", "init");
        RunGitInRepo("branch", "staging");

        RunGitInBare("init", "--bare", "-b", "main");
        RunGitInRepo("remote", "add", "origin", _bareRemotePath);
        RunGitInRepo("push", "-u", "origin", "main", "staging");
        await _gitWorkspaceService.FetchAsync(_repoPath, CancellationToken.None);
    }

    private string RunGitInRepo(params string[] args) => RunGit(_repoPath, args);

    private string RunGitInBare(params string[] args) => RunGit(_bareRemotePath, args);

    private string RunGitInWorktree(string worktreePath, params string[] args) => RunGit(worktreePath, args);

    private static string? TryGetGitConfig(string workingDirectory, string key)
    {
        using var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.ArgumentList.Add("config");
        process.StartInfo.ArgumentList.Add("--get");
        process.StartInfo.ArgumentList.Add(key);
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        string stdout = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 ? stdout.Trim() : null;
    }

    private static string RunGit(string workingDirectory, IReadOnlyList<string> args)
    {
        using var process = new System.Diagnostics.Process();
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
        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"git {string.Join(' ', args)} failed ({process.ExitCode}): {stderr}{stdout}");
        }

        string output = stdout.Trim();
        return string.IsNullOrWhiteSpace(output) ? stderr.Trim() : output;
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
