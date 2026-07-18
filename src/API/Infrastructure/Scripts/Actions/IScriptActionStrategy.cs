using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Scripts.Actions;

public sealed record ScriptActionContext(
    Guid ChatId,
    string Mode,
    bool Succeeded,
    string WorkspacePath,
    Guid? ProjectId,
    Guid? ParentChatId,
    Guid? WorkspaceId,
    string? Branch,
    string? BaseBranch,
    GitHostProviderSnapshot? GitHost,
    ScriptStepDto Step,
    Func<string, CancellationToken, Task<string?>>? GenerateCommitMessageAsync = null);

public sealed record GitHostProviderSnapshot(
    GitHostProvider Provider,
    string DefaultBaseBranch,
    string DefaultWorktreeBranchPattern);

public sealed record ScriptActionResult(
    bool Succeeded,
    string Label,
    string? Output = null,
    string? Error = null,
    Guid? SwitchToWorkspaceId = null,
    string? SwitchToWorkspacePath = null,
    string? SwitchToBranch = null);

public interface IScriptActionStrategy
{
    string Kind { get; }

    Task<ScriptActionResult> ExecuteAsync(ScriptActionContext context, CancellationToken cancellationToken);
}
