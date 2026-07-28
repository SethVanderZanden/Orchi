using Orchi.Api.Infrastructure.Cli;
using Orchi.Api.Infrastructure.Git.Workspace;
using Orchi.Api.Infrastructure.Scripts;

namespace Orchi.Api.Tests.Infrastructure.Agents;

public class AgentWorkspaceAutoCommitTests : IDisposable
{
    private readonly string _workspacePath;
    private readonly GitWorkspaceService _gitWorkspaceService;
    private readonly GitCommitMessageGenerator _commitMessageGenerator;

    public AgentWorkspaceAutoCommitTests()
    {
        _workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-auto-commit-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workspacePath);
        _gitWorkspaceService = new GitWorkspaceService(new ProcessRunner());
        _commitMessageGenerator = new GitCommitMessageGenerator(new ProcessRunner());
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
    public async Task AutoCommitFlow_CommitsTrackedChangesWithGeneratedMessage()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        await InitializeRepoAsync();
        File.WriteAllText(Path.Combine(_workspacePath, "feature.cs"), "class Feature {}\n");

        string? message = await _commitMessageGenerator.GenerateAsync(_workspacePath, CancellationToken.None);
        Assert.NotNull(message);
        Assert.Contains("feature.cs", message, StringComparison.Ordinal);

        await _gitWorkspaceService.CommitAsync(_workspacePath, message, CancellationToken.None);

        string status = await _gitWorkspaceService.GetStatusPorcelainAsync(_workspacePath, CancellationToken.None);
        Assert.Empty(status);
    }

    private async Task InitializeRepoAsync()
    {
        await _gitWorkspaceService.IsGitRepositoryAsync(_workspacePath, CancellationToken.None);
        RunGit("init");
        File.WriteAllText(Path.Combine(_workspacePath, "readme.txt"), "base\n");
        RunGit("add", "readme.txt");
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
