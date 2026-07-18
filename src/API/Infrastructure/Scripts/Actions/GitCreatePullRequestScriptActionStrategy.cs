using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Git.Hosting;
using Orchi.Api.Infrastructure.Git.Workspace;

namespace Orchi.Api.Infrastructure.Scripts.Actions;

public sealed class GitCreatePullRequestScriptActionStrategy(
    IGitHostingFacade hostingFacade,
    IGitWorkspaceService gitWorkspaceService) : IScriptActionStrategy
{
    public string Kind => ScriptStepKinds.GitCreatePullRequest;

    public async Task<ScriptActionResult> ExecuteAsync(
        ScriptActionContext context,
        CancellationToken cancellationToken)
    {
        const string label = "Creating pull request";

        GitHostProvider provider = context.GitHost?.Provider ?? GitHostProvider.GitHub;
        string head = context.Step.SourceBranch
            ?? context.Branch
            ?? await gitWorkspaceService.GetCurrentBranchAsync(context.WorkspacePath, cancellationToken)
            ?? string.Empty;
        string @base = context.Step.TargetBranch
            ?? context.BaseBranch
            ?? context.GitHost?.DefaultBaseBranch
            ?? "main";

        if (string.IsNullOrWhiteSpace(head))
        {
            return new ScriptActionResult(false, label, Error: "Head branch is required to create a pull request.");
        }

        string title = string.IsNullOrWhiteSpace(context.Step.Title)
            ? $"Orchi: {head}"
            : context.Step.Title.Trim();
        string body = context.Step.Body?.Trim() ?? "Created by Orchi scripting workflow.";

        try
        {
            CreatePullRequestResult result = await hostingFacade.CreatePullRequestAsync(
                provider,
                new CreatePullRequestRequest(
                    context.WorkspacePath,
                    title,
                    body,
                    head,
                    @base),
                cancellationToken);

            return new ScriptActionResult(true, label, result.Url);
        }
        catch (InvalidOperationException ex)
        {
            return new ScriptActionResult(false, label, Error: ex.Message);
        }
    }
}
