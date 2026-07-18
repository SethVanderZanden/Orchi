using System.Runtime.CompilerServices;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Scripts.Actions;

namespace Orchi.Api.Infrastructure.Scripts;

public sealed class ScriptEventDispatcher(
    IScriptStore scriptStore,
    IScriptActionStrategyFactory strategyFactory,
    IGitCommitMessageGenerator commitMessageGenerator,
    ILogger<ScriptEventDispatcher> logger) : IScriptEventDispatcher
{
    public async IAsyncEnumerable<AgentEvent> DispatchAsync(
        ScriptEventKind eventKind,
        ScriptDispatchContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        IReadOnlyList<StoredScript> scripts = await scriptStore.ListMatchingAsync(
            eventKind,
            context.ProjectId,
            context.Mode,
            cancellationToken);

        foreach (StoredScript script in scripts)
        {
            StoredScriptBinding? binding = script.Bindings.FirstOrDefault();
            ScriptOnError onError = binding?.OnError ?? ScriptOnError.Continue;

            yield return new AgentScriptEvent(
                "started",
                script.Name,
                $"Running {script.Name} script",
                null,
                null);

            bool scriptFailed = false;
            string? scriptError = null;

            foreach (ScriptStepDto step in ScriptStepsSerializer.Deserialize(script.StepsJson))
            {
                IScriptActionStrategy? strategy = null;
                string? strategyError = null;
                try
                {
                    strategy = strategyFactory.GetStrategy(step.Kind);
                }
                catch (InvalidOperationException ex)
                {
                    strategyError = ex.Message;
                }

                if (strategy is null)
                {
                    scriptFailed = true;
                    scriptError = strategyError ?? $"Unknown step kind '{step.Kind}'.";
                    yield return new AgentScriptEvent("failed", script.Name, step.Kind, null, scriptError);
                    break;
                }

                var actionContext = new ScriptActionContext(
                    context.ChatId,
                    context.Mode,
                    context.Succeeded,
                    context.WorkspacePath,
                    context.ProjectId,
                    context.ParentChatId,
                    context.WorkspaceId,
                    context.Branch,
                    context.BaseBranch,
                    context.GitHost,
                    step,
                    commitMessageGenerator.GenerateAsync);

                yield return new AgentScriptEvent(
                    "running",
                    script.Name,
                    DescribeStep(step),
                    null,
                    null);

                ScriptActionResult result = await strategy.ExecuteAsync(actionContext, cancellationToken);
                if (result.Succeeded)
                {
                    if (result.SwitchToWorkspaceId is Guid switchedId
                        && !string.IsNullOrWhiteSpace(result.SwitchToWorkspacePath))
                    {
                        context.WorkspaceId = switchedId;
                        context.WorkspacePath = result.SwitchToWorkspacePath;
                        context.Branch = result.SwitchToBranch ?? context.Branch;
                        context.WorkspaceSwitched = true;
                    }

                    yield return new AgentScriptEvent(
                        "stepCompleted",
                        script.Name,
                        result.Label,
                        result.Output,
                        null);
                    continue;
                }

                scriptFailed = true;
                scriptError = result.Error ?? result.Output ?? "Script step failed.";
                logger.LogWarning(
                    "Script {ScriptName} step {Kind} failed for chat {ChatId}: {Error}",
                    script.Name,
                    step.Kind,
                    context.ChatId,
                    scriptError);

                yield return new AgentScriptEvent(
                    "failed",
                    script.Name,
                    result.Label,
                    result.Output,
                    scriptError);
                break;
            }

            if (!scriptFailed)
            {
                yield return new AgentScriptEvent(
                    "completed",
                    script.Name,
                    $"Finished {script.Name} script",
                    null,
                    null);
                continue;
            }

            if (onError == ScriptOnError.AbortTurn && eventKind == ScriptEventKind.AgentStart)
            {
                yield return new AgentErrorEvent(
                    "Script.Aborted",
                    $"Script '{script.Name}' failed and aborted the turn: {scriptError}");
                yield break;
            }
        }
    }

    private static string DescribeStep(ScriptStepDto step) =>
        step.Kind switch
        {
            ScriptStepKinds.Shell => $"Running {step.Command}",
            ScriptStepKinds.GitCommit => "Committing changes",
            ScriptStepKinds.GitPush => "Pushing branch",
            ScriptStepKinds.GitMerge => "Merging branch",
            ScriptStepKinds.GitCreatePullRequest => "Creating pull request",
            ScriptStepKinds.GitWorktree => "Creating worktree",
            _ => step.Kind
        };
}
