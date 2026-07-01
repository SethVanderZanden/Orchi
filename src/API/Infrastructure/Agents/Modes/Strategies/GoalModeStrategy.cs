using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;

namespace Orchi.Api.Infrastructure.Agents.Modes.Strategies;

public sealed class GoalModeStrategy(AgentPromptComposer promptComposer) : IChatModeStrategy
{
    public ChatMode Mode => ChatMode.Goal;

    public ValueTask<Result<AgentTurnRequest>> PrepareTurnAsync(
        ChatSession session,
        string userContent,
        IPlanStore plans,
        CancellationToken cancellationToken)
    {
        bool isCheckIn = userContent.StartsWith("[goal-check-in]", StringComparison.Ordinal);
        string instructions = isCheckIn ? ModeInstructions.GoalCheckIn : ModeInstructions.Goal;
        return new(promptComposer.ComposeAsync(session, userContent, instructions, null, cancellationToken));
    }

    public async ValueTask OnTurnCompletedAsync(
        ChatSession session,
        AgentCompletedEvent completed,
        IPlanStore plans,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(completed.FullText))
        {
            return;
        }

        session.GoalJournal.Add(completed.FullText.Trim());
        await Task.CompletedTask;
    }

    public ValueTask OnChildActivityAsync(
        ChatSession parentSession,
        Coordination.ChatActivityEvent activity,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
