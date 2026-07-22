namespace Orchi.Api.Infrastructure.Git.Workspace;

public sealed record GitBranchInfo(string Name, bool IsCurrent, bool IsRemote);

public sealed record GitWorktreeCreateResult(string Path, string Branch, string BaseBranch);

public interface IGitWorkspaceService
{
    Task<bool> IsGitRepositoryAsync(string workspacePath, CancellationToken cancellationToken);

    /// <summary>
    /// Best-effort <c>git fetch --prune</c> so remote-tracking branches are current.
    /// </summary>
    Task FetchAsync(string workspacePath, CancellationToken cancellationToken);

    Task<IReadOnlyList<GitBranchInfo>> ListBranchesAsync(
        string workspacePath,
        CancellationToken cancellationToken,
        bool includeRemotes = true);

    Task<string?> GetCurrentBranchAsync(string workspacePath, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a user-facing branch name to a local ref usable by git (local branch or remote-tracking).
    /// </summary>
    Task<string?> ResolveBranchRefAsync(
        string workspacePath,
        string branchName,
        CancellationToken cancellationToken);

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

    /// <summary>
    /// Adds a worktree checked out at an existing branch (local or remote-tracking) for review.
    /// </summary>
    Task<GitWorktreeCreateResult> CreateWorktreeForExistingBranchAsync(
        string repositoryPath,
        string worktreeId,
        string headBranch,
        string baseBranch,
        CancellationToken cancellationToken);

    Task<string> GetStatusPorcelainAsync(string workspacePath, CancellationToken cancellationToken);
}
