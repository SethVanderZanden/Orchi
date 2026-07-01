using System.Threading.Channels;

namespace Orchi.SharedContext.Events;

public interface IWorkspaceEventBus
{
    ValueTask PublishAsync(WorkspaceEvent workspaceEvent, CancellationToken cancellationToken);

    ChannelReader<WorkspaceEvent> SubscribeAll();
}
