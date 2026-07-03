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
    public void Truncate_AppendsNoticeWhenDiffTooLarge()
    {
        string large = new string('a', GitWorkspaceDiffProvider.MaxDiffChars + 10);

        string truncated = GitWorkspaceDiffProvider.Truncate(large);

        Assert.Contains("[diff truncated", truncated);
        Assert.True(truncated.Length <= large.Length + 128);
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
