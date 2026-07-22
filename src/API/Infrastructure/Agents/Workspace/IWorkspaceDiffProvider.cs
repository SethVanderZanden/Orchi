namespace Orchi.Api.Infrastructure.Agents.Workspace;

public interface IWorkspaceDiffProvider
{
    string GetDiff(string workspacePath);

    string GetBranchDiff(string workspacePath, string baseBranch, string headBranch);
}
