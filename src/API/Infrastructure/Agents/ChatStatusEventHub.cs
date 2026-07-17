using System.Collections.Concurrent;
using System.Threading.Channels;
using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Agents;

public sealed record ChatStatusChangedEvent(Guid ChatId, ChatStatus Status);

public sealed class ChatStatusEventHub
{
    private readonly ConcurrentDictionary<Guid, Channel<ChatStatusChangedEvent>> _subscribers = new();

    public (Guid SubscriptionId, ChannelReader<ChatStatusChangedEvent> Reader) Subscribe()
    {
        Guid subscriptionId = Guid.NewGuid();
        Channel<ChatStatusChangedEvent> channel = Channel.CreateUnbounded<ChatStatusChangedEvent>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

        _subscribers[subscriptionId] = channel;
        return (subscriptionId, channel.Reader);
    }

    public void Unsubscribe(Guid subscriptionId)
    {
        if (!_subscribers.TryRemove(subscriptionId, out Channel<ChatStatusChangedEvent>? channel))
        {
            return;
        }

        channel.Writer.TryComplete();
    }

    public async Task PublishAsync(ChatStatusChangedEvent statusEvent, CancellationToken cancellationToken)
    {
        foreach (Channel<ChatStatusChangedEvent> channel in _subscribers.Values)
        {
            await channel.Writer.WriteAsync(statusEvent, cancellationToken);
        }
    }
}
