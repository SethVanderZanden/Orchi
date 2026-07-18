using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Scripts.Actions;

namespace Orchi.Api.Infrastructure.Scripts;

/// <summary>
/// Mutable dispatch context so AgentStart steps (e.g. git.worktree) can switch
/// the chat onto a new workspace before the CLI adapter runs.
/// </summary>
public sealed class ScriptDispatchContext
{
    public required Guid ChatId { get; init; }

    public required string Mode { get; init; }

    public required bool Succeeded { get; init; }

    public required string WorkspacePath { get; set; }

    public Guid? ProjectId { get; init; }

    public Guid? ParentChatId { get; init; }

    public Guid? WorkspaceId { get; set; }

    public string? Branch { get; set; }

    public string? BaseBranch { get; set; }

    public GitHostProviderSnapshot? GitHost { get; init; }

    public bool WorkspaceSwitched { get; set; }
}

public interface IScriptEventDispatcher
{
    IAsyncEnumerable<AgentEvent> DispatchAsync(
        ScriptEventKind eventKind,
        ScriptDispatchContext context,
        CancellationToken cancellationToken);
}
