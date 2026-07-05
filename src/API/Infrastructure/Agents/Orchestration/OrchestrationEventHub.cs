using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Orchi.Api.Infrastructure.Agents.Orchestration;

public sealed class OrchestrationEventHub
{
    private readonly ConcurrentDictionary<Guid, Channel<OrchestrationEvent>> _channels = new();

    public ChannelReader<OrchestrationEvent> Subscribe(Guid parentChatId)
    {
        Channel<OrchestrationEvent> channel = _channels.GetOrAdd(
            parentChatId,
            _ => Channel.CreateUnbounded<OrchestrationEvent>(new UnboundedChannelOptions
            {
                SingleReader = false,
                SingleWriter = false
            }));

        return channel.Reader;
    }

    public void Unsubscribe(Guid parentChatId, ChannelReader<OrchestrationEvent> reader)
    {
        if (!_channels.TryGetValue(parentChatId, out Channel<OrchestrationEvent>? channel))
        {
            return;
        }

        if (channel.Reader != reader)
        {
            return;
        }

        _channels.TryRemove(parentChatId, out _);
    }

    public async Task PublishAsync(
        Guid parentChatId,
        OrchestrationEvent orchestrationEvent,
        CancellationToken cancellationToken)
    {
        if (!_channels.TryGetValue(parentChatId, out Channel<OrchestrationEvent>? channel))
        {
            return;
        }

        await channel.Writer.WriteAsync(orchestrationEvent, cancellationToken);
    }
}
