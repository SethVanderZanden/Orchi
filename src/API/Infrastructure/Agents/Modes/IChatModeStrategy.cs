using System.Threading.Channels;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;

namespace Orchi.Api.Infrastructure.Agents.Modes;

public interface IChatModeStrategy
{
    ChatMode Mode { get; }

    Result<AgentTurnRequest> PrepareTurn(ChatSession session, string userContent, IPlanStore plans);

    ValueTask OnTurnCompletedAsync(
        ChatSession session,
        AgentCompletedEvent completed,
        IPlanStore plans,
        CancellationToken cancellationToken);

    ValueTask OnChildActivityAsync(
        ChatSession parentSession,
        Coordination.ChatActivityEvent activity,
        CancellationToken cancellationToken);
}
