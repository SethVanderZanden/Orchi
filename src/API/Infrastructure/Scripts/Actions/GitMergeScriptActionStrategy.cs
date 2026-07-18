using Orchi.Api.Infrastructure.Git.Workspace;

namespace Orchi.Api.Infrastructure.Scripts.Actions;

public sealed class GitMergeScriptActionStrategy(IGitWorkspaceService gitWorkspaceService) : IScriptActionStrategy
{
    public string Kind => ScriptStepKinds.GitMerge;

    public async Task<ScriptActionResult> ExecuteAsync(
        ScriptActionContext context,
        CancellationToken cancellationToken)
    {
        string source = context.Step.SourceBranch
            ?? context.Branch
            ?? await gitWorkspaceService.GetCurrentBranchAsync(context.WorkspacePath, cancellationToken)
            ?? string.Empty;

        string target = context.Step.TargetBranch
            ?? context.BaseBranch
            ?? context.GitHost?.DefaultBaseBranch
            ?? "main";

        string label = $"Merging {source} into {target}";

        if (string.IsNullOrWhiteSpace(source))
        {
            return new ScriptActionResult(false, label, Error: "Source branch is required for merge.");
        }

        try
        {
            // Merge in the repository root when possible (parent/primary path may differ from worktree).
            await gitWorkspaceService.MergeAsync(context.WorkspacePath, source, target, cancellationToken);
            return new ScriptActionResult(true, label, $"Merged {source} into {target}.");
        }
        catch (InvalidOperationException ex)
        {
            return new ScriptActionResult(false, label, Error: ex.Message);
        }
    }
}
