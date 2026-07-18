using Orchi.Api.Infrastructure.Git.Workspace;

namespace Orchi.Api.Infrastructure.Scripts.Actions;

public sealed class GitCommitScriptActionStrategy(IGitWorkspaceService gitWorkspaceService) : IScriptActionStrategy
{
    public string Kind => ScriptStepKinds.GitCommit;

    public async Task<ScriptActionResult> ExecuteAsync(
        ScriptActionContext context,
        CancellationToken cancellationToken)
    {
        const string label = "Committing changes";

        try
        {
            string message = context.Step.Message?.Trim() ?? string.Empty;
            if (context.Step.GenerateMessage || string.IsNullOrWhiteSpace(message))
            {
                if (context.GenerateCommitMessageAsync is null)
                {
                    message = string.IsNullOrWhiteSpace(message)
                        ? "chore: orchi agent changes"
                        : message;
                }
                else
                {
                    string? generated = await context.GenerateCommitMessageAsync(
                        context.WorkspacePath,
                        cancellationToken);
                    message = string.IsNullOrWhiteSpace(generated)
                        ? (string.IsNullOrWhiteSpace(context.Step.Message)
                            ? "chore: orchi agent changes"
                            : context.Step.Message.Trim())
                        : generated.Trim();
                }
            }

            await gitWorkspaceService.CommitAsync(context.WorkspacePath, message, cancellationToken);
            return new ScriptActionResult(true, label, $"Committed with message: {message}");
        }
        catch (InvalidOperationException ex)
        {
            return new ScriptActionResult(false, label, Error: ex.Message);
        }
    }
}
