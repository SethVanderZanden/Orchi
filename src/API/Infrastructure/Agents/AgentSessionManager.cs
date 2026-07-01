using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Orchi.Api.Common.Results;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Modes.Coordination;
using Orchi.Api.Infrastructure.Agents.Modes.Plan;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.SharedContext.Events;
using Orchi.SharedContext.Modes;
using Orchi.SharedContext.Session;

namespace Orchi.Api.Infrastructure.Agents;

public sealed class AgentSessionManager
{
    private readonly ConcurrentDictionary<Guid, ChatSession> _sessions = new();
    private readonly IChatStore _chatStore;
    private readonly IAgentAdapterFactory _adapterFactory;
    private readonly ChatModeStrategyFactory _modeStrategyFactory;
    private readonly PlanManager _planManager;
    private readonly IPlanStore _planStore;
    private readonly GoalCheckInQueue _goalCheckInQueue;
    private readonly IModeRuntime _modeRuntime;
    private readonly IWorkspaceEventBus _eventBus;
    private readonly ISessionDistiller _sessionDistiller;
    private readonly ILogger<AgentSessionManager> _logger;

    public AgentSessionManager(
        IChatStore chatStore,
        IAgentAdapterFactory adapterFactory,
        ChatModeStrategyFactory modeStrategyFactory,
        PlanManager planManager,
        IPlanStore planStore,
        GoalCheckInQueue goalCheckInQueue,
        IModeRuntime modeRuntime,
        IWorkspaceEventBus eventBus,
        ISessionDistiller sessionDistiller,
        ILogger<AgentSessionManager> logger)
    {
        _chatStore = chatStore;
        _adapterFactory = adapterFactory;
        _modeStrategyFactory = modeStrategyFactory;
        _planManager = planManager;
        _planStore = planStore;
        _goalCheckInQueue = goalCheckInQueue;
        _modeRuntime = modeRuntime;
        _eventBus = eventBus;
        _sessionDistiller = sessionDistiller;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ChatSession>> ListSessionsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatSession> fromStore = await _chatStore.ListAsync(cancellationToken);
        var merged = new Dictionary<Guid, ChatSession>();
        foreach (ChatSession session in fromStore)
        {
            merged[session.Id] = session;
        }

        foreach (ChatSession cached in _sessions.Values)
        {
            merged[cached.Id] = cached;
        }

        return merged.Values
            .OrderByDescending(session => session.Messages.LastOrDefault()?.CreatedAt ?? DateTimeOffset.MinValue)
            .ToList();
    }

    public ChatSession? GetSession(Guid chatId) =>
        _sessions.TryGetValue(chatId, out ChatSession? session) ? session : null;

    public async Task<ChatSession?> GetOrLoadSessionAsync(Guid chatId, CancellationToken cancellationToken)
    {
        if (_sessions.TryGetValue(chatId, out ChatSession? cached))
        {
            return cached;
        }

        ChatSession? loaded = await _chatStore.GetAsync(chatId, cancellationToken);
        if (loaded is null)
        {
            return null;
        }

        _sessions[chatId] = loaded;
        return loaded;
    }

    public async Task<Result<ChatSession>> CreateSessionAsync(
        string agentId,
        string workspacePath,
        ChatMode mode = ChatMode.Agent,
        Guid? parentChatId = null,
        Guid? attachedPlanId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return Result.Failure<ChatSession>(Error.Validation("Agent.Required", "Agent is required."));
        }

        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            return Result.Failure<ChatSession>(Error.Validation("Workspace.Required", "Workspace path is required."));
        }

        string fullPath = Path.GetFullPath(workspacePath);

        if (!Directory.Exists(fullPath))
        {
            return Result.Failure<ChatSession>(Error.Validation("Workspace.NotFound", $"Workspace path does not exist: {fullPath}"));
        }

        try
        {
            _ = _adapterFactory.GetAdapter(agentId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ChatSession>(Error.Validation("Agent.Unsupported", ex.Message));
        }

        if (parentChatId is not null && !await _chatStore.ExistsAsync(parentChatId.Value, cancellationToken))
        {
            return Result.Failure<ChatSession>(Error.Validation("ParentChat.NotFound", $"Parent chat '{parentChatId}' was not found."));
        }

        if (mode == ChatMode.Implement)
        {
            Result planValidation = _planManager.ValidateAttachedPlan(attachedPlanId);
            if (planValidation.IsFailure)
            {
                return Result.Failure<ChatSession>(planValidation.Error);
            }
        }

        var sessionId = Guid.NewGuid();
        ChatSession session = await _chatStore.CreateAsync(
            new ChatCreateModel(sessionId, agentId, fullPath, mode, parentChatId, attachedPlanId),
            cancellationToken);

        _sessions[session.Id] = session;
        _ = TriggerWorkspaceIndexIfStaleAsync(session.WorkspacePath);
        return Result.Success(session);
    }

    public async Task<Result> SetGoalChatIdAsync(
        Guid orchestratorChatId,
        Guid goalChatId,
        CancellationToken cancellationToken)
    {
        ChatSession? orchestrator = await GetOrLoadSessionAsync(orchestratorChatId, cancellationToken);
        if (orchestrator is null)
        {
            return Result.Failure(Error.NotFound($"Chat '{orchestratorChatId}' was not found."));
        }

        if (!await _chatStore.ExistsAsync(goalChatId, cancellationToken))
        {
            return Result.Failure(Error.Validation("GoalChat.NotFound", $"Goal chat '{goalChatId}' was not found."));
        }

        orchestrator.GoalChatId = goalChatId;
        await _chatStore.UpdateGoalChatIdAsync(orchestratorChatId, goalChatId, cancellationToken);
        return Result.Success();
    }

    public async Task<Result<ChatSession>> UpdateModeAsync(
        Guid chatId,
        ChatMode mode,
        Guid? attachedPlanId,
        CancellationToken cancellationToken)
    {
        ChatSession? session = await GetOrLoadSessionAsync(chatId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        lock (session.Sync)
        {
            if (session.RunningProcess is not null)
            {
                return Result.Failure<ChatSession>(
                    Error.Validation("Chat.Busy", "Cannot change mode while a message is in progress."));
            }
        }

        Guid? resolvedPlanId = mode == ChatMode.Implement ? attachedPlanId : null;

        if (mode == ChatMode.Implement)
        {
            Result planValidation = _planManager.ValidateAttachedPlan(resolvedPlanId);
            if (planValidation.IsFailure)
            {
                return Result.Failure<ChatSession>(planValidation.Error);
            }
        }

        ChatMode previousMode = session.Mode;
        string previousModeKey = ChatModeParser.ToApiString(previousMode);

        session.Mode = mode;
        session.AttachedPlanId = resolvedPlanId;

        if (previousMode != mode)
        {
            session.PreviousModeKey = previousModeKey;
            session.ModeChangedAt = DateTimeOffset.UtcNow;

            if (!_modeRuntime.ShouldPreserveResume(previousModeKey, ChatModeParser.ToApiString(mode)))
            {
                session.ExternalSessionId = null;
            }
        }

        await _chatStore.UpdateModeAsync(chatId, mode, resolvedPlanId, cancellationToken);

        await _eventBus.PublishAsync(
            new WorkspaceEvent(
                WorkspaceEventKind.ModeChanged,
                session.WorkspacePath,
                DateTimeOffset.UtcNow,
                chatId,
                $"{previousModeKey}->{ChatModeParser.ToApiString(mode)}"),
            cancellationToken);

        return Result.Success(session);
    }

    public async Task<Result> CloseSessionAsync(Guid chatId, CancellationToken cancellationToken)
    {
        if (_sessions.TryRemove(chatId, out ChatSession? session))
        {
            StopRunningProcess(session);
        }

        bool deleted = await _chatStore.DeleteAsync(chatId, cancellationToken);
        if (!deleted)
        {
            return Result.Failure(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        return Result.Success();
    }

    public async Task CloseAllSessionsAsync(CancellationToken cancellationToken)
    {
        foreach (Guid chatId in _sessions.Keys.ToList())
        {
            if (_sessions.TryRemove(chatId, out ChatSession? session))
            {
                StopRunningProcess(session);
            }
        }

        await Task.CompletedTask;
    }

    public async Task<ChatMessage> AppendUserMessageAsync(
        Guid chatId,
        string content,
        CancellationToken cancellationToken)
    {
        ChatSession session = await GetRequiredSessionAsync(chatId, cancellationToken);
        var message = new ChatMessage(
            Guid.NewGuid(),
            "user",
            content,
            DateTimeOffset.UtcNow);

        lock (session.Sync)
        {
            session.Messages.Add(message);
        }

        await _chatStore.SaveUserMessageAsync(chatId, message, cancellationToken);
        PublishChildActivity(session, ChatActivityKind.ChildUserMessage, "user", content);
        return message;
    }

    public async Task SeedInitialMessageAsync(Guid chatId, string content, CancellationToken cancellationToken)
    {
        ChatSession session = await GetRequiredSessionAsync(chatId, cancellationToken);
        var message = new ChatMessage(
            Guid.NewGuid(),
            "user",
            content,
            DateTimeOffset.UtcNow);

        lock (session.Sync)
        {
            session.Messages.Add(message);
        }

        await _chatStore.SaveUserMessageAsync(chatId, message, cancellationToken);
    }

    public async IAsyncEnumerable<AgentEvent> SendMessageAsync(
        Guid chatId,
        string content,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (AgentEvent agentEvent in ExecuteTurnAsync(chatId, content, publishUserMessage: true, cancellationToken))
        {
            yield return agentEvent;
        }
    }

    public async IAsyncEnumerable<AgentEvent> SendInternalMessageAsync(
        Guid chatId,
        string content,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (AgentEvent agentEvent in ExecuteTurnAsync(chatId, content, publishUserMessage: false, cancellationToken))
        {
            yield return agentEvent;
        }
    }

    private async IAsyncEnumerable<AgentEvent> ExecuteTurnAsync(
        Guid chatId,
        string content,
        bool publishUserMessage,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ChatSession session = await GetRequiredSessionAsync(chatId, cancellationToken);
        IAgentAdapter adapter = _adapterFactory.GetAdapter(session.AgentId);
        IChatModeStrategy strategy = _modeStrategyFactory.GetStrategy(session.Mode);

        StopRunningProcess(session);

        ChatMessage userMessage;
        if (publishUserMessage)
        {
            userMessage = await AppendUserMessageAsync(chatId, content, cancellationToken);
        }
        else
        {
            userMessage = new ChatMessage(
                Guid.NewGuid(),
                "user",
                content,
                DateTimeOffset.UtcNow);

            lock (session.Sync)
            {
                session.Messages.Add(userMessage);
            }

            await _chatStore.SaveUserMessageAsync(chatId, userMessage, cancellationToken);
        }

        Result<AgentTurnRequest> turnResult = await strategy.PrepareTurnAsync(session, content, _planStore, cancellationToken);
        if (turnResult.IsFailure)
        {
            var errorAssistantMessage = new ChatMessage(
                Guid.NewGuid(),
                "assistant",
                turnResult.Error.Message,
                DateTimeOffset.UtcNow,
                Status: "error");

            lock (session.Sync)
            {
                session.Messages.Add(errorAssistantMessage);
            }

            await _chatStore.SaveAssistantMessageAsync(chatId, errorAssistantMessage, null, cancellationToken);
            yield return new AgentErrorEvent(turnResult.Error.Code, turnResult.Error.Message);
            yield break;
        }

        AgentTurnRequest turn = turnResult.Value;

        var runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        session.RunCts = runCts;

        yield return new AgentStatusEvent("processing");

        var assistantMessage = new ChatMessage(
            Guid.NewGuid(),
            "assistant",
            string.Empty,
            DateTimeOffset.UtcNow,
            Status: "processing");

        lock (session.Sync)
        {
            session.Messages.Add(assistantMessage);
        }

        bool turnCompleted = false;

        await foreach (AgentEvent agentEvent in adapter.SendMessageAsync(
                           session,
                           turn.PreparedPrompt,
                           turn.ExtraCliArgs,
                           runCts.Token))
        {
            switch (agentEvent)
            {
                case AgentSessionStartedEvent started:
                    await PersistExternalSessionIdAsync(
                        chatId,
                        session,
                        started.ExternalSessionId,
                        "system-init",
                        cancellationToken);
                    break;

                case AgentTextDeltaEvent delta:
                    lock (session.Sync)
                    {
                        assistantMessage = assistantMessage with
                        {
                            Content = assistantMessage.Content + delta.Text,
                            Status = "streaming"
                        };
                        session.Messages[^1] = assistantMessage;
                    }

                    yield return delta;
                    break;

                case AgentCompletedEvent completed:
                    string? externalSessionId;
                    lock (session.Sync)
                    {
                        if (!string.IsNullOrEmpty(completed.FullText))
                        {
                            assistantMessage = assistantMessage with { Content = completed.FullText };
                        }

                        assistantMessage = assistantMessage with { Status = "complete" };
                        session.Messages[^1] = assistantMessage;

                        if (!string.IsNullOrWhiteSpace(completed.ExternalSessionId))
                        {
                            session.ExternalSessionId = completed.ExternalSessionId;
                        }

                        externalSessionId = session.ExternalSessionId;
                    }

                    if (!string.IsNullOrWhiteSpace(completed.ExternalSessionId))
                    {
                        _logger.LogDebug(
                            "Captured external session id from {Source} for chat {ChatId}",
                            "result",
                            chatId);
                    }

                    await _chatStore.SaveAssistantMessageAsync(
                        chatId,
                        assistantMessage,
                        externalSessionId,
                        cancellationToken);

                    await strategy.OnTurnCompletedAsync(session, completed, _planStore, cancellationToken);
                    PublishChildActivity(session, ChatActivityKind.ChildMessageCompleted, "assistant", assistantMessage.Content);
                    await PublishTurnCompletedAsync(session, userMessage.Content, assistantMessage.Content, cancellationToken);
                    turnCompleted = true;
                    yield return completed;
                    break;

                case AgentErrorEvent error:
                    lock (session.Sync)
                    {
                        assistantMessage = assistantMessage with
                        {
                            Content = string.IsNullOrEmpty(assistantMessage.Content) ? error.Message : assistantMessage.Content,
                            Status = "error"
                        };
                        session.Messages[^1] = assistantMessage;
                    }

                    await _chatStore.SaveAssistantMessageAsync(chatId, assistantMessage, null, cancellationToken);
                    turnCompleted = true;
                    yield return error;
                    break;

                default:
                    yield return agentEvent;
                    break;
            }
        }

        if (!turnCompleted)
        {
            bool shouldFinalizeIncomplete;
            string? externalSessionId;
            lock (session.Sync)
            {
                shouldFinalizeIncomplete = assistantMessage.Status is "processing" or "streaming";
                if (!shouldFinalizeIncomplete)
                {
                    externalSessionId = null;
                }
                else
                {
                    assistantMessage = assistantMessage with
                    {
                        Content = string.IsNullOrEmpty(assistantMessage.Content)
                            ? "Agent finished without a result event."
                            : assistantMessage.Content,
                        Status = "error"
                    };
                    session.Messages[^1] = assistantMessage;
                    externalSessionId = session.ExternalSessionId;
                }
            }

            if (shouldFinalizeIncomplete)
            {
                _logger.LogWarning(
                    "Agent stream ended without result event for chat {ChatId}",
                    chatId);

                await _chatStore.SaveAssistantMessageAsync(
                    chatId,
                    assistantMessage,
                    externalSessionId,
                    cancellationToken);

                yield return new AgentErrorEvent(
                    "Agent.Incomplete",
                    "Agent finished without a result event.");
            }
        }

        session.RunCts = null;
    }

    private async Task PersistExternalSessionIdAsync(
        Guid chatId,
        ChatSession session,
        string externalSessionId,
        string source,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(externalSessionId))
        {
            return;
        }

        lock (session.Sync)
        {
            session.ExternalSessionId = externalSessionId;
        }

        await _chatStore.UpdateExternalSessionIdAsync(chatId, externalSessionId, cancellationToken);
        _logger.LogDebug(
            "Captured external session id from {Source} for chat {ChatId}",
            source,
            chatId);
    }

    private void PublishChildActivity(ChatSession childSession, ChatActivityKind kind, string role, string content)
    {
        if (childSession.ParentChatId is not Guid parentChatId)
        {
            return;
        }

        ChatSession? parent = GetSession(parentChatId);
        if (parent is null || parent.Mode != ChatMode.Goal)
        {
            return;
        }

        _goalCheckInQueue.Enqueue(new GoalCheckInRequest(
            parentChatId,
            new ChatActivityEvent(childSession.Id, childSession.Mode, kind, role, content)));

        _ = _eventBus.PublishAsync(
            new WorkspaceEvent(
                WorkspaceEventKind.ChatActivity,
                childSession.WorkspacePath,
                DateTimeOffset.UtcNow,
                childSession.Id,
                kind.ToString()),
            CancellationToken.None);
    }

    private async Task PublishTurnCompletedAsync(
        ChatSession session,
        string userContent,
        string assistantContent,
        CancellationToken cancellationToken)
    {
        await _sessionDistiller.DistillTurnAsync(
            session.WorkspacePath,
            session.Id,
            userContent,
            assistantContent,
            cancellationToken);

        await _eventBus.PublishAsync(
            new WorkspaceEvent(
                WorkspaceEventKind.TurnCompleted,
                session.WorkspacePath,
                DateTimeOffset.UtcNow,
                session.Id),
            cancellationToken);
    }

    private async Task TriggerWorkspaceIndexIfStaleAsync(string workspacePath)
    {
        try
        {
            await _eventBus.PublishAsync(
                new WorkspaceEvent(
                    WorkspaceEventKind.WorkspaceIndexed,
                    workspacePath,
                    DateTimeOffset.UtcNow),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to enqueue workspace index for {WorkspacePath}", workspacePath);
        }
    }

    private async Task<ChatSession> GetRequiredSessionAsync(Guid chatId, CancellationToken cancellationToken) =>
        await GetOrLoadSessionAsync(chatId, cancellationToken)
        ?? throw new KeyNotFoundException($"Chat '{chatId}' was not found.");

    private void StopRunningProcess(ChatSession session)
    {
        lock (session.Sync)
        {
            try
            {
                session.RunCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            session.RunCts?.Dispose();
            session.RunCts = null;

            Process? process = session.RunningProcess;
            session.RunningProcess = null;

            if (process is null)
            {
                return;
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to kill agent process for chat {ChatId}", session.Id);
            }
            finally
            {
                process.Dispose();
            }
        }
    }
}
