using System.Threading.Channels;

namespace Orchi.SharedContext.Events;

internal sealed class InProcessWorkspaceEventBus : IWorkspaceEventBus
{
    private readonly Channel<WorkspaceEvent> _channel = Channel.CreateUnbounded<WorkspaceEvent>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    public ChannelReader<WorkspaceEvent> SubscribeAll() => _channel.Reader;

    public async ValueTask PublishAsync(WorkspaceEvent workspaceEvent, CancellationToken cancellationToken) =>
        await _channel.Writer.WriteAsync(workspaceEvent, cancellationToken);
}
