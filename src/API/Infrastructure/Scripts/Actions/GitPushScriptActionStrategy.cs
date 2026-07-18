using Orchi.Api.Infrastructure.Git.Workspace;

namespace Orchi.Api.Infrastructure.Scripts.Actions;

public sealed class GitPushScriptActionStrategy(IGitWorkspaceService gitWorkspaceService) : IScriptActionStrategy
{
    public string Kind => ScriptStepKinds.GitPush;

    public async Task<ScriptActionResult> ExecuteAsync(
        ScriptActionContext context,
        CancellationToken cancellationToken)
    {
        const string label = "Pushing branch";

        try
        {
            await gitWorkspaceService.PushAsync(
                context.WorkspacePath,
                context.Step.SetUpstream,
                cancellationToken);

            return new ScriptActionResult(true, label, "Push completed.");
        }
        catch (InvalidOperationException ex)
        {
            return new ScriptActionResult(false, label, Error: ex.Message);
        }
    }
}
