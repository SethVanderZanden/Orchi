using System.Threading.Channels;

namespace Orchi.Api.Infrastructure.Agents.Modes.Coordination;

public sealed record GoalCheckInRequest(Guid GoalChatId, ChatActivityEvent Activity);

public sealed class GoalCheckInQueue
{
    private readonly Channel<GoalCheckInRequest> _channel =
        Channel.CreateUnbounded<GoalCheckInRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<GoalCheckInRequest> Reader => _channel.Reader;

    public void Enqueue(GoalCheckInRequest request) => _channel.Writer.TryWrite(request);
}
