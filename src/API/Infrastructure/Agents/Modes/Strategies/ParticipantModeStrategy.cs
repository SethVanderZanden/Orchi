using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;

namespace Orchi.Api.Infrastructure.Agents.Modes.Strategies;

public sealed class ParticipantModeStrategy(AgentPromptComposer promptComposer) : IChatModeStrategy
{
    public ChatMode Mode => ChatMode.Participant;

    public ValueTask<Result<AgentTurnRequest>> PrepareTurnAsync(
        ChatSession session,
        string userContent,
        IPlanStore plans,
        CancellationToken cancellationToken) =>
        new(promptComposer.ComposeAsync(session, userContent, ModeInstructions.Participant, null, cancellationToken));

    public ValueTask OnTurnCompletedAsync(
        ChatSession session,
        AgentCompletedEvent completed,
        IPlanStore plans,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;

    public ValueTask OnChildActivityAsync(
        ChatSession parentSession,
        Coordination.ChatActivityEvent activity,
        CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
