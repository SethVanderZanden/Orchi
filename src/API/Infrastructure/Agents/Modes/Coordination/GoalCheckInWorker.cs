using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Coordination;
using Orchi.Api.Infrastructure.Agents.Modes.Strategies;

namespace Orchi.Api.Infrastructure.Agents.Modes.Coordination;

public sealed class GoalCheckInWorker(
    GoalCheckInQueue queue,
    AgentSessionManager sessionManager,
    ChatModeStrategyFactory strategyFactory,
    ILogger<GoalCheckInWorker> logger) : BackgroundService
{
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromSeconds(5);
    private readonly Dictionary<Guid, (DateTimeOffset LastEnqueued, GoalCheckInRequest Request)> _pending = new();
    private readonly object _pendingSync = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await queue.Reader.WaitToReadAsync(stoppingToken);

                while (queue.Reader.TryRead(out GoalCheckInRequest? request))
                {
                    lock (_pendingSync)
                    {
                        _pending[request.GoalChatId] = (DateTimeOffset.UtcNow, request);
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
                logger.LogError(ex, "Goal check-in worker failed.");
            }
        }
    }

    private async Task FlushPendingAsync(CancellationToken cancellationToken)
    {
        List<GoalCheckInRequest> toProcess;
        lock (_pendingSync)
        {
            DateTimeOffset cutoff = DateTimeOffset.UtcNow - DebounceWindow;
            toProcess = _pending
                .Where(pair => pair.Value.LastEnqueued <= cutoff)
                .Select(pair => pair.Value.Request)
                .ToList();

            foreach (GoalCheckInRequest request in toProcess)
            {
                _pending.Remove(request.GoalChatId);
            }
        }

        foreach (GoalCheckInRequest request in toProcess)
        {
            await ProcessCheckInAsync(request, cancellationToken);
        }
    }

    private async Task ProcessCheckInAsync(GoalCheckInRequest request, CancellationToken cancellationToken)
    {
        ChatSession? goalSession = await sessionManager.GetOrLoadSessionAsync(request.GoalChatId, cancellationToken);
        if (goalSession is null || goalSession.Mode != ChatMode.Goal)
        {
            return;
        }

        if (goalSession.RunningProcess is not null)
        {
            return;
        }

        IChatModeStrategy strategy = strategyFactory.GetStrategy(ChatMode.Goal);
        await strategy.OnChildActivityAsync(goalSession, request.Activity, cancellationToken);

        string prompt =
            $"""
            [goal-check-in]
            Child chat: {request.Activity.ChildChatId}
            Child mode: {ChatModeParser.ToApiString(request.Activity.ChildMode)}
            Activity: {request.Activity.Kind}
            Last message ({request.Activity.LastMessageRole}):
            {request.Activity.LastMessageContent}
            """;

        try
        {
            await foreach (AgentEvent _ in sessionManager.SendInternalMessageAsync(
                               request.GoalChatId,
                               prompt,
                               cancellationToken))
            {
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed goal check-in for chat {GoalChatId}", request.GoalChatId);
        }
    }
}
