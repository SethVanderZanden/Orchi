namespace Orchi.Api.Infrastructure.Git.Workspace;

public sealed record GitBranchInfo(string Name, bool IsCurrent);

public sealed record GitWorktreeCreateResult(string Path, string Branch, string BaseBranch);

public interface IGitWorkspaceService
{
    Task<bool> IsGitRepositoryAsync(string workspacePath, CancellationToken cancellationToken);

    Task<IReadOnlyList<GitBranchInfo>> ListBranchesAsync(string workspacePath, CancellationToken cancellationToken);

    Task<string?> GetCurrentBranchAsync(string workspacePath, CancellationToken cancellationToken);

    Task CommitAsync(
        string workspacePath,
        string message,
        CancellationToken cancellationToken);

    Task PushAsync(
        string workspacePath,
        bool setUpstream,
        CancellationToken cancellationToken);

    Task MergeAsync(
        string workspacePath,
        string sourceBranch,
        string targetBranch,
        CancellationToken cancellationToken);

    Task<GitWorktreeCreateResult> CreateWorktreeAsync(
        string repositoryPath,
        string planId,
        string baseBranch,
        string? branchName,
        CancellationToken cancellationToken);

    Task<string> GetStatusPorcelainAsync(string workspacePath, CancellationToken cancellationToken);
}
