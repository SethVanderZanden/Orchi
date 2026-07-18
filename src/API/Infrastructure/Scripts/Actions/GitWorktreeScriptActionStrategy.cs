using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Git.Workspace;
using Orchi.Api.Infrastructure.Projects;

namespace Orchi.Api.Infrastructure.Scripts.Actions;

public sealed class GitWorktreeScriptActionStrategy(
    IGitWorkspaceService gitWorkspaceService,
    IProjectStore projectStore) : IScriptActionStrategy
{
    public string Kind => ScriptStepKinds.GitWorktree;

    public async Task<ScriptActionResult> ExecuteAsync(
        ScriptActionContext context,
        CancellationToken cancellationToken)
    {
        const string label = "Creating worktree";

        if (context.ProjectId is null)
        {
            return new ScriptActionResult(false, label, Error: "Project is required to register a worktree workspace.");
        }

        if (context.WorkspaceId is Guid currentWorkspaceId)
        {
            Workspace? current = await projectStore.GetWorkspaceAsync(currentWorkspaceId, cancellationToken);
            if (current is { Kind: WorkspaceKind.Worktree })
            {
                return new ScriptActionResult(
                    true,
                    label,
                    $"Already on worktree branch {current.Branch ?? current.Name}.");
            }
        }

        Project? project = await projectStore.GetProjectAsync(context.ProjectId.Value, cancellationToken);
        if (project is null)
        {
            return new ScriptActionResult(false, label, Error: "Project was not found.");
        }

        Workspace? primary = project.Workspaces.FirstOrDefault(workspace => workspace.IsDefault)
            ?? project.Workspaces.FirstOrDefault(workspace => workspace.Kind == WorkspaceKind.Primary)
            ?? project.Workspaces.FirstOrDefault();

        if (primary is null)
        {
            return new ScriptActionResult(false, label, Error: "Project has no primary workspace for worktree creation.");
        }

        string baseBranch = context.Step.BaseBranch
            ?? context.BaseBranch
            ?? context.GitHost?.DefaultBaseBranch
            ?? project.DefaultBaseBranch
            ?? "main";

        string branchName = string.IsNullOrWhiteSpace(context.Step.Branch)
            ? WorktreeBranchPattern.Resolve(
                context.GitHost?.DefaultWorktreeBranchPattern ?? project.DefaultWorktreeBranchPattern,
                context.ChatId,
                context.Mode)
            : context.Step.Branch.Trim();

        try
        {
            GitWorktreeCreateResult created = await gitWorkspaceService.CreateWorktreeAsync(
                primary.Path,
                planId: branchName,
                baseBranch,
                branchName,
                cancellationToken);

            WorkspaceCreateResult? workspace = await projectStore.CreateWorkspaceAsync(
                context.ProjectId.Value,
                created.Path,
                created.Branch,
                WorkspaceKind.Worktree,
                created.Branch,
                created.BaseBranch,
                cancellationToken);

            if (workspace is null)
            {
                return new ScriptActionResult(false, label, Error: "Failed to register worktree workspace.");
            }

            return new ScriptActionResult(
                true,
                label,
                $"Worktree at {created.Path} on branch {created.Branch}",
                SwitchToWorkspaceId: workspace.Workspace.Id,
                SwitchToWorkspacePath: workspace.Workspace.Path,
                SwitchToBranch: created.Branch);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return new ScriptActionResult(false, label, Error: ex.Message);
        }
    }
}
