using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Git.Workspace;
using Orchi.Api.Infrastructure.Projects;
using Orchi.Api.Infrastructure.Scripts;
using Orchi.Api.Infrastructure.Scripts.Actions;

namespace Orchi.Api.Tests.Infrastructure.Scripts.Actions;

public class GitWorktreeScriptActionStrategyTests
{
    [Fact]
    public async Task ExecuteAsync_ReviewMode_SkipsWorktreeCreation()
    {
        var strategy = new GitWorktreeScriptActionStrategy(
            new ThrowingGitWorkspaceService(),
            new ThrowingProjectStore());

        var context = new ScriptActionContext(
            Guid.NewGuid(),
            AgentModeIds.Review,
            Succeeded: true,
            "/workspace",
            Guid.NewGuid(),
            ParentChatId: Guid.NewGuid(),
            WorkspaceId: Guid.NewGuid(),
            Branch: null,
            BaseBranch: null,
            GitHost: null,
            new ScriptStepDto(ScriptStepKinds.GitWorktree));

        ScriptActionResult result = await strategy.ExecuteAsync(context, CancellationToken.None);

        Assert.True(result.Succeeded);
        Assert.Null(result.SwitchToWorkspaceId);
        Assert.Contains("reuses", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ThrowingGitWorkspaceService : IGitWorkspaceService
    {
        public Task<bool> IsGitRepositoryAsync(string workspacePath, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task FetchAsync(string workspacePath, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<IReadOnlyList<GitBranchInfo>> ListBranchesAsync(
            string workspacePath,
            CancellationToken cancellationToken,
            bool includeRemotes = true) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<string?> GetCurrentBranchAsync(string workspacePath, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<string> ResolveRepositoryRootAsync(string workspacePath, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<string?> ResolveBranchRefAsync(
            string workspacePath,
            string branchName,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task CommitAsync(string workspacePath, string message, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task PushAsync(string workspacePath, bool setUpstream, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task MergeAsync(
            string workspacePath,
            string sourceBranch,
            string targetBranch,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<GitWorktreeCreateResult> CreateWorktreeAsync(
            string repositoryPath,
            string planId,
            string baseBranch,
            string? branchName,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<GitWorktreeCreateResult> CreateWorktreeForExistingBranchAsync(
            string repositoryPath,
            string worktreeId,
            string headBranch,
            string baseBranch,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<string> GetStatusPorcelainAsync(string workspacePath, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");
    }

    private sealed class ThrowingProjectStore : IProjectStore
    {
        public Task<Project?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<IReadOnlyList<Project>> ListProjectsAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<Workspace?> GetWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<ProjectCreateResult> CreateProjectAsync(
            string name,
            string defaultWorkspacePath,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<Project?> UpdateProjectAsync(
            Guid projectId,
            string? name,
            string? defaultBaseBranch,
            string? defaultWorktreeBranchPattern,
            GitHostProvider? gitHostProvider,
            bool? useWorktreeOnKickoff,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<ProjectDeleteResult?> DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<WorkspaceCreateResult?> CreateWorkspaceAsync(
            Guid projectId,
            string path,
            string? name,
            WorkspaceKind kind,
            string? branch,
            string? baseBranch,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<Workspace?> UpdateWorkspaceAsync(
            Guid workspaceId,
            string? name,
            bool? isDefault,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");

        public Task<bool> DeleteWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Should not be called for review mode.");
    }
}
