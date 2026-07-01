using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orchi.SharedContext.Events;
using Orchi.SharedContext.Indexing;
using Orchi.SharedContext.Storage;

namespace Orchi.SharedContext.Events;

internal sealed class WorkspaceIndexWorker(
    IWorkspaceEventBus eventBus,
    IProjectIndexer indexer,
    IContextStore contextStore,
    ILogger<WorkspaceIndexWorker> logger) : BackgroundService
{
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromSeconds(10);
    private readonly Dictionary<string, DateTimeOffset> _pending = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _sync = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ChannelReader<WorkspaceEvent> reader = eventBus.SubscribeAll();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await reader.WaitToReadAsync(stoppingToken);

                while (reader.TryRead(out WorkspaceEvent? workspaceEvent))
                {
                    if (!ShouldTriggerIndex(workspaceEvent.Kind))
                    {
                        continue;
                    }

                    lock (_sync)
                    {
                        _pending[workspaceEvent.WorkspacePath] = DateTimeOffset.UtcNow;
                    }
                }

                await Task.Delay(DebounceWindow, stoppingToken);
                await FlushPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Workspace index worker failed.");
            }
        }
    }

    private async Task FlushPendingAsync(CancellationToken cancellationToken)
    {
        List<string> workspaces;
        lock (_sync)
        {
            DateTimeOffset cutoff = DateTimeOffset.UtcNow - DebounceWindow;
            workspaces = _pending
                .Where(pair => pair.Value <= cutoff)
                .Select(pair => pair.Key)
                .ToList();

            foreach (string workspace in workspaces)
            {
                _pending.Remove(workspace);
            }
        }

        foreach (string workspacePath in workspaces)
        {
            await IndexIfStaleAsync(workspacePath, cancellationToken);
        }
    }

    internal async Task IndexIfStaleAsync(string workspacePath, CancellationToken cancellationToken)
    {
        WorkspaceContext? workspace = await contextStore.GetWorkspaceAsync(workspacePath, cancellationToken);
        if (!indexer.IsStale(workspacePath, workspace?.LastIndexedAt))
        {
            return;
        }

        try
        {
            await indexer.IndexAsync(workspacePath, new IndexOptions(), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to index workspace {WorkspacePath}", workspacePath);
        }
    }

    private static bool ShouldTriggerIndex(WorkspaceEventKind kind) =>
        kind is WorkspaceEventKind.FileChanged
            or WorkspaceEventKind.TurnCompleted
            or WorkspaceEventKind.MergeCompleted
            or WorkspaceEventKind.TaskCompleted
            or WorkspaceEventKind.WorkspaceIndexed;
}
