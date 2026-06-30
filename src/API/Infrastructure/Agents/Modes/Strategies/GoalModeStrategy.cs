using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;
using Orchi.Api.Infrastructure.Agents.Persistence;

namespace Orchi.Api.Infrastructure.Agents.Modes.Strategies;

public sealed class GoalModeStrategy(IChatStore chatStore) : IChatModeStrategy
{
    public ChatMode Mode => ChatMode.Goal;

    public Result<AgentTurnRequest> PrepareTurn(ChatSession session, string userContent, IPlanStore plans)
    {
        bool isCheckIn = userContent.StartsWith("[goal-check-in]", StringComparison.Ordinal);
        string instructions = isCheckIn ? ModeInstructions.GoalCheckIn : ModeInstructions.Goal;
        string prepared = $"{instructions}\n\n---\n\n{userContent.Trim()}";
        IReadOnlyList<string> extraArgs = isCheckIn ? ["--mode=ask"] : ["--mode=plan"];

        return Result.Success(new AgentTurnRequest(prepared, extraArgs));
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

        string entry = $"- {DateTimeOffset.UtcNow:u}: {completed.FullText.Trim()}";

        lock (session.Sync)
        {
            session.GoalJournal.Add(entry);
        }

        await chatStore.AppendGoalJournalAsync(session.Id, entry, cancellationToken);
    }

    public ValueTask OnChildActivityAsync(
        ChatSession parentSession,
        Coordination.ChatActivityEvent activity,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
