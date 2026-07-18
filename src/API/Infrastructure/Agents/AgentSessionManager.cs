using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Models;
using Orchi.Api.Infrastructure.Agents.Orchestration;
using Orchi.Api.Infrastructure.Projects;
using Orchi.Api.Infrastructure.Scripts;
using Orchi.Api.Infrastructure.Scripts.Actions;

namespace Orchi.Api.Infrastructure.Agents;

public sealed class AgentSessionManager
{
    private readonly ConcurrentDictionary<Guid, ChatSession> _sessions = new();
    private readonly IChatStore _chatStore;
    private readonly IProjectStore _projectStore;
    private readonly IAgentAdapterFactory _adapterFactory;
    private readonly IAgentModeStrategyFactory _modeStrategyFactory;
    private readonly IAgentModelCatalogService _modelCatalogService;
    private readonly IModeRuntimeDefaultService _modeDefaultService;
    private readonly IAgentContextSizeCatalogService _contextSizeCatalogService;
    private readonly IAgentCliOptionCatalogService _cliOptionCatalogService;
    private readonly IAgentPromptComposer _promptComposer;
    private readonly IAgentTurnCompletionNotifier _turnCompletionNotifier;
    private readonly IChatStatusService _chatStatusService;
    private readonly IScriptEventDispatcher _scriptEventDispatcher;
    private readonly ILogger<AgentSessionManager> _logger;

    public AgentSessionManager(
        IChatStore chatStore,
        IProjectStore projectStore,
        IAgentAdapterFactory adapterFactory,
        IAgentModeStrategyFactory modeStrategyFactory,
        IAgentModelCatalogService modelCatalogService,
        IModeRuntimeDefaultService modeDefaultService,
        IAgentContextSizeCatalogService contextSizeCatalogService,
        IAgentCliOptionCatalogService cliOptionCatalogService,
        IAgentPromptComposer promptComposer,
        IAgentTurnCompletionNotifier turnCompletionNotifier,
        IChatStatusService chatStatusService,
        IScriptEventDispatcher scriptEventDispatcher,
        ILogger<AgentSessionManager> logger)
    {
        _chatStore = chatStore;
        _projectStore = projectStore;
        _adapterFactory = adapterFactory;
        _modeStrategyFactory = modeStrategyFactory;
        _modelCatalogService = modelCatalogService;
        _modeDefaultService = modeDefaultService;
        _contextSizeCatalogService = contextSizeCatalogService;
        _cliOptionCatalogService = cliOptionCatalogService;
        _promptComposer = promptComposer;
        _turnCompletionNotifier = turnCompletionNotifier;
        _chatStatusService = chatStatusService;
        _scriptEventDispatcher = scriptEventDispatcher;
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
            merged[cached.Id] = MergeCachedSessionForList(cached, merged);
        }

        return merged.Values
            .OrderByDescending(session => session.Messages.LastOrDefault()?.CreatedAt ?? DateTimeOffset.MinValue)
            .ToList();
    }

    public ChatSession? GetSession(Guid chatId) =>
        _sessions.TryGetValue(chatId, out ChatSession? session) ? session : null;

    public void DetachProjectLinks(IReadOnlyList<Guid> chatIds)
    {
        foreach (Guid chatId in chatIds)
        {
            if (!_sessions.TryGetValue(chatId, out ChatSession? session))
            {
                continue;
            }

            lock (session.Sync)
            {
                session.ProjectId = null;
                session.WorkspaceId = null;
            }
        }
    }

    public async Task<ChatSession?> GetOrLoadSessionAsync(Guid chatId, CancellationToken cancellationToken)
    {
        if (_sessions.TryGetValue(chatId, out ChatSession? cached))
        {
            return cached;
        }

        ChatSession? loaded = await _chatStore.GetAsync(chatId, cancellationToken);
        if (loaded is null)
        {
            _sessions.TryRemove(chatId, out _);
            return null;
        }

        await HydrateCliConfigAsync(loaded, cancellationToken);
        _sessions[chatId] = loaded;
        return loaded;
    }

    public async Task<IReadOnlyList<ChatSession>> ListChildSessionsAsync(
        Guid parentChatId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatSession> fromStore =
            await _chatStore.ListChildrenAsync(parentChatId, cancellationToken);
        var merged = new Dictionary<Guid, ChatSession>();
        foreach (ChatSession session in fromStore)
        {
            merged[session.Id] = session;
        }

        foreach (ChatSession cached in _sessions.Values)
        {
            if (cached.ParentChatId == parentChatId)
            {
                merged[cached.Id] = cached;
            }
        }

        return merged.Values
            .OrderByDescending(session => session.Messages.LastOrDefault()?.CreatedAt ?? DateTimeOffset.MinValue)
            .ToArray();
    }

    public async Task<Result<ChatSession>> CreateSessionAsync(
        Guid workspaceId,
        string? agentId = null,
        string? mode = null,
        Guid? parentChatId = null,
        string? planFilePath = null,
        string? modelId = null,
        string? contextSizeId = null,
        string? reasoningEffortId = null,
        string? approvalPolicyId = null,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
        {
            return Result.Failure<ChatSession>(Error.Validation("Workspace.Required", "Workspace id is required."));
        }

        string resolvedMode = string.IsNullOrWhiteSpace(mode) ? DefaultAgentModeStrategy.Mode : mode.Trim();

        try
        {
            _ = _modeStrategyFactory.GetStrategy(resolvedMode);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ChatSession>(Error.Validation("Mode.Unsupported", ex.Message));
        }

        ModeRuntimeResolution defaults = await _modeDefaultService.ResolveAsync(resolvedMode, cancellationToken);

        string resolvedAgentId = string.IsNullOrWhiteSpace(agentId) ? defaults.AgentId : agentId.Trim();
        string? resolvedModelId = string.IsNullOrWhiteSpace(modelId) ? defaults.ModelId : modelId.Trim();
        string? resolvedContextSizeId = string.IsNullOrWhiteSpace(contextSizeId)
            ? defaults.ContextSizeId
            : contextSizeId.Trim();
        string? resolvedReasoningEffortId = string.IsNullOrWhiteSpace(reasoningEffortId)
            ? defaults.ReasoningEffortId
            : reasoningEffortId.Trim();
        string? resolvedApprovalPolicyId = string.IsNullOrWhiteSpace(approvalPolicyId)
            ? defaults.ApprovalPolicyId
            : approvalPolicyId.Trim();

        Entities.Workspace? workspace = await _projectStore.GetWorkspaceAsync(workspaceId, cancellationToken);
        if (workspace is null)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Workspace '{workspaceId}' was not found."));
        }

        string fullPath = workspace.Path;

        if (!Directory.Exists(fullPath))
        {
            return Result.Failure<ChatSession>(Error.Validation("Workspace.NotFound", $"Workspace path does not exist: {fullPath}"));
        }

        try
        {
            _ = _adapterFactory.GetAdapter(resolvedAgentId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ChatSession>(Error.Validation("Agent.Unsupported", ex.Message));
        }

        Result validation = await ValidateRuntimeSelectionAsync(
            resolvedAgentId,
            resolvedModelId,
            resolvedContextSizeId,
            resolvedReasoningEffortId,
            resolvedApprovalPolicyId,
            cancellationToken);

        if (validation.IsFailure)
        {
            return Result.Failure<ChatSession>(validation.Error);
        }

        var sessionId = Guid.NewGuid();
        ChatSession session = await _chatStore.CreateAsync(
            new ChatCreateModel(
                sessionId,
                resolvedAgentId,
                fullPath,
                resolvedMode,
                parentChatId,
                planFilePath,
                workspace.ProjectId,
                workspace.Id,
                resolvedModelId,
                resolvedContextSizeId,
                resolvedReasoningEffortId,
                resolvedApprovalPolicyId),
            cancellationToken);

        await HydrateCliConfigAsync(session, cancellationToken);
        _sessions[session.Id] = session;
        return Result.Success(session);
    }

    public async Task<Result<ChatSession>> UpdateModeAsync(
        Guid chatId,
        string? mode,
        CancellationToken cancellationToken)
    {
        ChatSession? session = await GetOrLoadSessionAsync(chatId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        lock (session.Sync)
        {
            if (session.RunningProcess is not null
                || session.RunCts is not null
                || session.Messages.Any(message =>
                    message.Role == "assistant" && message.Status is "processing" or "streaming"))
            {
                return Result.Failure<ChatSession>(
                    Error.Validation("Mode.Busy", "Mode cannot be changed while the agent is running."));
            }
        }

        string resolvedMode = string.IsNullOrWhiteSpace(mode) ? DefaultAgentModeStrategy.Mode : mode.Trim();

        try
        {
            _ = _modeStrategyFactory.GetStrategy(resolvedMode);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ChatSession>(Error.Validation("Mode.Unsupported", ex.Message));
        }

        ModeRuntimeResolution defaults = await _modeDefaultService.ResolveAsync(resolvedMode, cancellationToken);

        Result validation = await ValidateRuntimeSelectionAsync(
            defaults.AgentId,
            defaults.ModelId,
            defaults.ContextSizeId,
            defaults.ReasoningEffortId,
            defaults.ApprovalPolicyId,
            cancellationToken);

        if (validation.IsFailure)
        {
            return Result.Failure<ChatSession>(validation.Error);
        }

        bool agentChanged = !string.Equals(session.AgentId, defaults.AgentId, StringComparison.OrdinalIgnoreCase);

        bool updated = await _chatStore.UpdateRuntimeAsync(
            chatId,
            defaults.AgentId,
            resolvedMode,
            defaults.ModelId,
            defaults.ContextSizeId,
            defaults.ReasoningEffortId,
            defaults.ApprovalPolicyId,
            clearExternalSessionId: agentChanged,
            cancellationToken);

        if (!updated)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        session.AgentId = defaults.AgentId;
        session.Mode = resolvedMode;
        session.ModelId = defaults.ModelId;
        session.ContextSizeId = defaults.ContextSizeId;
        session.ReasoningEffortId = defaults.ReasoningEffortId;
        session.ApprovalPolicyId = defaults.ApprovalPolicyId;

        if (agentChanged)
        {
            session.ExternalSessionId = null;
        }

        await HydrateCliConfigAsync(session, cancellationToken);
        _sessions[chatId] = session;
        return Result.Success(session);
    }

    public async Task<Result<ChatSession>> MarkReadAsync(
        Guid chatId,
        CancellationToken cancellationToken)
    {
        // Only keep InProgress while a turn is actually running. Idle InProgress
        // (missed ReadyForReview after disconnect) must clear so open → read works.
        Result<ChatSession> result = IsSessionActivelyRunning(chatId)
            ? await _chatStatusService.TouchLastReadAsync(chatId, cancellationToken)
            : await _chatStatusService.MarkReadAsync(chatId, cancellationToken);

        if (result.IsFailure)
        {
            return result;
        }

        ChatSession session = result.Value;
        ApplyStatusToCachedSession(chatId, session.Status, session.LastReadAt);
        return result;
    }

    public async Task<Result<ChatSession>> UpdateModelAsync(
        Guid chatId,
        string? modelId,
        CancellationToken cancellationToken)
    {
        ChatSession? session = await GetOrLoadSessionAsync(chatId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        lock (session.Sync)
        {
            if (session.RunningProcess is not null
                || session.RunCts is not null
                || session.Messages.Any(message =>
                    message.Role == "assistant" && message.Status is "processing" or "streaming"))
            {
                return Result.Failure<ChatSession>(
                    Error.Validation("Model.Busy", "Model cannot be changed while the agent is running."));
            }
        }

        string? resolvedModelId = string.IsNullOrWhiteSpace(modelId) ? null : modelId.Trim();

        if (resolvedModelId is not null)
        {
            bool enabled = await _modelCatalogService.IsEnabledModelAsync(
                session.AgentId,
                resolvedModelId,
                cancellationToken);

            if (!enabled)
            {
                return Result.Failure<ChatSession>(
                    Error.Validation("Model.Unsupported", $"Model '{resolvedModelId}' is not available."));
            }
        }

        bool updated = await _chatStore.UpdateModelIdAsync(chatId, resolvedModelId, cancellationToken);
        if (!updated)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        session.ModelId = resolvedModelId;
        _sessions[chatId] = session;
        return Result.Success(session);
    }

    public async Task<Result<ChatSession>> UpdateContextSizeAsync(
        Guid chatId,
        string? contextSizeId,
        CancellationToken cancellationToken)
    {
        ChatSession? session = await GetOrLoadSessionAsync(chatId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        lock (session.Sync)
        {
            if (session.RunningProcess is not null
                || session.RunCts is not null
                || session.Messages.Any(message =>
                    message.Role == "assistant" && message.Status is "processing" or "streaming"))
            {
                return Result.Failure<ChatSession>(
                    Error.Validation("ContextSize.Busy", "Context size cannot be changed while the agent is running."));
            }
        }

        string? resolvedContextSizeId = string.IsNullOrWhiteSpace(contextSizeId) ? null : contextSizeId.Trim();

        if (resolvedContextSizeId is not null)
        {
            bool enabled = await _contextSizeCatalogService.IsEnabledSizeAsync(
                session.AgentId,
                resolvedContextSizeId,
                cancellationToken);

            if (!enabled)
            {
                return Result.Failure<ChatSession>(
                    Error.Validation(
                        "ContextSize.Unsupported",
                        $"Context size '{resolvedContextSizeId}' is not available."));
            }
        }

        bool updated = await _chatStore.UpdateContextSizeIdAsync(chatId, resolvedContextSizeId, cancellationToken);
        if (!updated)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        session.ContextSizeId = resolvedContextSizeId;
        await HydrateCliConfigAsync(session, cancellationToken);
        _sessions[chatId] = session;
        return Result.Success(session);
    }

    public async Task<Result<ChatSession>> UpdateReasoningEffortAsync(
        Guid chatId,
        string? reasoningEffortId,
        CancellationToken cancellationToken)
    {
        ChatSession? session = await GetOrLoadSessionAsync(chatId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        lock (session.Sync)
        {
            if (session.RunningProcess is not null
                || session.RunCts is not null
                || session.Messages.Any(message =>
                    message.Role == "assistant" && message.Status is "processing" or "streaming"))
            {
                return Result.Failure<ChatSession>(
                    Error.Validation(
                        "ReasoningEffort.Busy",
                        "Reasoning effort cannot be changed while the agent is running."));
            }
        }

        string? resolvedReasoningEffortId =
            string.IsNullOrWhiteSpace(reasoningEffortId) ? null : reasoningEffortId.Trim();

        if (resolvedReasoningEffortId is not null)
        {
            bool enabled = await _cliOptionCatalogService.IsEnabledOptionAsync(
                session.AgentId,
                AgentCliOptionKinds.ModelReasoningEffort,
                resolvedReasoningEffortId,
                cancellationToken);

            if (!enabled)
            {
                return Result.Failure<ChatSession>(
                    Error.Validation(
                        "ReasoningEffort.Unsupported",
                        $"Reasoning effort '{resolvedReasoningEffortId}' is not available."));
            }
        }

        bool updated = await _chatStore.UpdateReasoningEffortIdAsync(
            chatId,
            resolvedReasoningEffortId,
            cancellationToken);

        if (!updated)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        session.ReasoningEffortId = resolvedReasoningEffortId;
        await HydrateCliConfigAsync(session, cancellationToken);
        _sessions[chatId] = session;
        return Result.Success(session);
    }

    public async Task<Result<ChatSession>> UpdateApprovalPolicyAsync(
        Guid chatId,
        string? approvalPolicyId,
        CancellationToken cancellationToken)
    {
        ChatSession? session = await GetOrLoadSessionAsync(chatId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        lock (session.Sync)
        {
            if (session.RunningProcess is not null
                || session.RunCts is not null
                || session.Messages.Any(message =>
                    message.Role == "assistant" && message.Status is "processing" or "streaming"))
            {
                return Result.Failure<ChatSession>(
                    Error.Validation(
                        "ApprovalPolicy.Busy",
                        "Approval policy cannot be changed while the agent is running."));
            }
        }

        string? resolvedApprovalPolicyId =
            string.IsNullOrWhiteSpace(approvalPolicyId) ? null : approvalPolicyId.Trim();

        if (resolvedApprovalPolicyId is not null)
        {
            bool enabled = await _cliOptionCatalogService.IsEnabledOptionAsync(
                session.AgentId,
                AgentCliOptionKinds.ApprovalPolicy,
                resolvedApprovalPolicyId,
                cancellationToken);

            if (!enabled)
            {
                return Result.Failure<ChatSession>(
                    Error.Validation(
                        "ApprovalPolicy.Unsupported",
                        $"Approval policy '{resolvedApprovalPolicyId}' is not available."));
            }
        }

        bool updated = await _chatStore.UpdateApprovalPolicyIdAsync(
            chatId,
            resolvedApprovalPolicyId,
            cancellationToken);

        if (!updated)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        session.ApprovalPolicyId = resolvedApprovalPolicyId;
        await HydrateCliConfigAsync(session, cancellationToken);
        _sessions[chatId] = session;
        return Result.Success(session);
    }

    public async Task<Result<ChatSession>> UpdateWorkspaceAsync(
        Guid chatId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        ChatSession? session = await GetOrLoadSessionAsync(chatId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        lock (session.Sync)
        {
            if (session.RunningProcess is not null
                || session.RunCts is not null
                || session.Messages.Any(message =>
                    message.Role == "assistant" && message.Status is "processing" or "streaming"))
            {
                return Result.Failure<ChatSession>(
                    Error.Validation(
                        "Workspace.Busy",
                        "Workspace cannot be changed while the agent is running."));
            }
        }

        Entities.Workspace? workspace = await _projectStore.GetWorkspaceAsync(workspaceId, cancellationToken);
        if (workspace is null)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Workspace '{workspaceId}' was not found."));
        }

        if (!Directory.Exists(workspace.Path))
        {
            return Result.Failure<ChatSession>(
                Error.Validation("Workspace.NotFound", $"Workspace path does not exist: {workspace.Path}"));
        }

        bool updated = await _chatStore.UpdateWorkspaceAsync(
            chatId,
            workspace.ProjectId,
            workspace.Id,
            workspace.Path,
            cancellationToken);

        if (!updated)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        session.ProjectId = workspace.ProjectId;
        session.WorkspaceId = workspace.Id;
        session.WorkspacePath = workspace.Path;
        _sessions[chatId] = session;
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
        return message;
    }

    public async Task SaveAssistantStatusMessageAsync(
        Guid chatId,
        ChatMessage message,
        CancellationToken cancellationToken)
    {
        await _chatStore.SaveStatusMessageAsync(chatId, message, cancellationToken);
    }

    public async IAsyncEnumerable<AgentEvent> SendMessageAsync(
        Guid chatId,
        string content,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ChatSession session = await GetRequiredSessionAsync(chatId, cancellationToken);
        IAgentAdapter adapter = _adapterFactory.GetAdapter(session.AgentId);

        StopRunningProcess(session);

        await AppendUserMessageAsync(chatId, content, cancellationToken);

        string composedPrompt = _promptComposer.Compose(session, content);
        IReadOnlyList<string> extraCliArgs = BuildCliArgs(session);

        var runCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        session.RunCts = runCts;

        await TransitionStatusAsync(chatId, session, ChatStatus.InProgress, cancellationToken);

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

        ScriptDispatchContext scriptContext = await BuildScriptDispatchContextAsync(
            session,
            succeeded: true,
            cancellationToken);

        bool abortTurn = false;
        await foreach (AgentEvent scriptEvent in _scriptEventDispatcher.DispatchAsync(
                           ScriptEventKind.AgentStart,
                           scriptContext,
                           runCts.Token))
        {
            yield return scriptEvent;
            if (scriptEvent is AgentErrorEvent)
            {
                abortTurn = true;
            }
        }

        if (scriptContext.WorkspaceSwitched
            && scriptContext.WorkspaceId is Guid switchedWorkspaceId
            && !string.IsNullOrWhiteSpace(scriptContext.WorkspacePath))
        {
            await ApplyWorkspaceSwitchAsync(
                chatId,
                session,
                switchedWorkspaceId,
                scriptContext.WorkspacePath,
                runCts.Token);
        }

        if (abortTurn)
        {
            lock (session.Sync)
            {
                assistantMessage = assistantMessage with
                {
                    Content = "Turn aborted by start script.",
                    Status = "error"
                };
                session.Messages[^1] = assistantMessage;
            }

            await _chatStore.SaveAssistantMessageAsync(chatId, assistantMessage, null, cancellationToken);
            await TransitionStatusAsync(chatId, session, ChatStatus.ReadyForReview, cancellationToken);
            yield return new AgentErrorEvent("Script.Aborted", "Turn aborted by start script.");
            yield break;
        }

        bool turnCompleted = false;

        await foreach (AgentEvent agentEvent in adapter.SendMessageAsync(
                           session,
                           composedPrompt,
                           extraCliArgs,
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

                    await TransitionStatusAsync(
                        chatId,
                        session,
                        ChatStatus.ReadyForReview,
                        cancellationToken);

                    turnCompleted = true;
                    await foreach (AgentEvent finishEvent in DispatchFinishScriptsAsync(
                                       session,
                                       succeeded: true,
                                       runCts.Token))
                    {
                        yield return finishEvent;
                    }

                    _turnCompletionNotifier.NotifyTurnCompleted(chatId, succeeded: true);
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

                    await TransitionStatusAsync(
                        chatId,
                        session,
                        ChatStatus.ReadyForReview,
                        cancellationToken);

                    turnCompleted = true;
                    await foreach (AgentEvent finishEvent in DispatchFinishScriptsAsync(
                                       session,
                                       succeeded: false,
                                       runCts.Token))
                    {
                        yield return finishEvent;
                    }

                    _turnCompletionNotifier.NotifyTurnCompleted(chatId, succeeded: false);
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

                await TransitionStatusAsync(
                    chatId,
                    session,
                    ChatStatus.ReadyForReview,
                    cancellationToken);

                yield return new AgentErrorEvent(
                    "Agent.Incomplete",
                    "Agent finished without a result event.");
            }
        }

        session.RunCts = null;
    }

    private async Task TransitionStatusAsync(
        Guid chatId,
        ChatSession session,
        ChatStatus status,
        CancellationToken cancellationToken)
    {
        // Status must persist even if the HTTP client disconnects mid-turn.
        // Using the request token left chats stuck InProgress after completion.
        _ = cancellationToken;

        if (status == ChatStatus.InProgress)
        {
            await _chatStatusService.SetInProgressAsync(chatId, CancellationToken.None);
        }
        else if (status == ChatStatus.ReadyForReview)
        {
            await _chatStatusService.SetReadyForReviewAsync(chatId, CancellationToken.None);
        }

        ApplyStatusToCachedSession(chatId, status, session.LastReadAt);
    }

    private static ChatSession MergeCachedSessionForList(
        ChatSession cached,
        IReadOnlyDictionary<Guid, ChatSession> fromStoreById)
    {
        if (!fromStoreById.TryGetValue(cached.Id, out ChatSession? fromStore))
        {
            return cached;
        }

        if (IsSessionActivelyRunning(cached))
        {
            cached.Status = ChatStatus.InProgress;
            return cached;
        }

        // Heal stale in-memory InProgress when DB already advanced.
        cached.Status = fromStore.Status;
        cached.LastReadAt = fromStore.LastReadAt;
        return cached;
    }

    private bool IsSessionActivelyRunning(Guid chatId) =>
        _sessions.TryGetValue(chatId, out ChatSession? session) && IsSessionActivelyRunning(session);

    private static bool IsSessionActivelyRunning(ChatSession session)
    {
        lock (session.Sync)
        {
            if (session.RunCts is not null || session.RunningProcess is not null)
            {
                return true;
            }

            return session.Messages.Any(message =>
                message.Role == "assistant" && message.Status is "processing" or "streaming");
        }
    }

    private void ApplyStatusToCachedSession(
        Guid chatId,
        ChatStatus status,
        DateTimeOffset? lastReadAt)
    {
        if (!_sessions.TryGetValue(chatId, out ChatSession? session))
        {
            return;
        }

        // A late mark-read snapshot must not clobber ReadyForReview / Read with InProgress.
        bool isDowngrade = status == ChatStatus.InProgress
            && session.Status is ChatStatus.ReadyForReview or ChatStatus.Read;

        if (!isDowngrade)
        {
            session.Status = status;
        }

        if (lastReadAt.HasValue)
        {
            session.LastReadAt = lastReadAt;
        }
    }

    private IReadOnlyList<string> BuildCliArgs(ChatSession session) =>
        _promptComposer.GetExtraCliArgs(session.Mode);

    private async Task<Result> ValidateRuntimeSelectionAsync(
        string agentId,
        string? modelId,
        string? contextSizeId,
        string? reasoningEffortId,
        string? approvalPolicyId,
        CancellationToken cancellationToken)
    {
        if (modelId is not null)
        {
            bool enabled = await _modelCatalogService.IsEnabledModelAsync(agentId, modelId, cancellationToken);
            if (!enabled)
            {
                return Result.Failure(
                    Error.Validation("Model.Unsupported", $"Model '{modelId}' is not available."));
            }
        }

        if (contextSizeId is not null)
        {
            bool enabled = await _contextSizeCatalogService.IsEnabledSizeAsync(
                agentId,
                contextSizeId,
                cancellationToken);

            if (!enabled)
            {
                return Result.Failure(
                    Error.Validation(
                        "ContextSize.Unsupported",
                        $"Context size '{contextSizeId}' is not available."));
            }
        }

        if (reasoningEffortId is not null)
        {
            bool enabled = await _cliOptionCatalogService.IsEnabledOptionAsync(
                agentId,
                AgentCliOptionKinds.ModelReasoningEffort,
                reasoningEffortId,
                cancellationToken);

            if (!enabled)
            {
                return Result.Failure(
                    Error.Validation(
                        "ReasoningEffort.Unsupported",
                        $"Reasoning effort '{reasoningEffortId}' is not available."));
            }
        }

        if (approvalPolicyId is not null)
        {
            bool enabled = await _cliOptionCatalogService.IsEnabledOptionAsync(
                agentId,
                AgentCliOptionKinds.ApprovalPolicy,
                approvalPolicyId,
                cancellationToken);

            if (!enabled)
            {
                return Result.Failure(
                    Error.Validation(
                        "ApprovalPolicy.Unsupported",
                        $"Approval policy '{approvalPolicyId}' is not available."));
            }
        }

        return Result.Success();
    }

    private async Task HydrateCliConfigAsync(ChatSession session, CancellationToken cancellationToken)
    {
        session.CliConfigOverrides.Clear();

        if (string.IsNullOrWhiteSpace(session.ContextSizeId))
        {
            session.ContextSizeTokens = null;
        }
        else
        {
            session.ContextSizeTokens = await _contextSizeCatalogService.ResolveTokenCountAsync(
                session.AgentId,
                session.ContextSizeId,
                cancellationToken);
        }

        if (session.ContextSizeTokens is > 0)
        {
            session.CliConfigOverrides["model_context_window"] = session.ContextSizeTokens.Value.ToString();
        }

        if (!string.IsNullOrWhiteSpace(session.ReasoningEffortId))
        {
            string? cliValue = await _cliOptionCatalogService.ResolveCliValueAsync(
                session.AgentId,
                AgentCliOptionKinds.ModelReasoningEffort,
                session.ReasoningEffortId,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(cliValue))
            {
                session.CliConfigOverrides[AgentCliOptionKinds.ModelReasoningEffort] = cliValue;
            }
        }

        if (!string.IsNullOrWhiteSpace(session.ApprovalPolicyId))
        {
            string? cliValue = await _cliOptionCatalogService.ResolveCliValueAsync(
                session.AgentId,
                AgentCliOptionKinds.ApprovalPolicy,
                session.ApprovalPolicyId,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(cliValue))
            {
                session.CliConfigOverrides[AgentCliOptionKinds.ApprovalPolicy] = cliValue;
            }
        }
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

    private async IAsyncEnumerable<AgentEvent> DispatchFinishScriptsAsync(
        ChatSession session,
        bool succeeded,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ScriptDispatchContext context = await BuildScriptDispatchContextAsync(session, succeeded, cancellationToken);
        await foreach (AgentEvent finishEvent in _scriptEventDispatcher.DispatchAsync(
                           ScriptEventKind.AgentFinish,
                           context,
                           cancellationToken))
        {
            yield return finishEvent;
        }
    }

    private async Task ApplyWorkspaceSwitchAsync(
        Guid chatId,
        ChatSession session,
        Guid workspaceId,
        string workspacePath,
        CancellationToken cancellationToken)
    {
        Entities.Workspace? workspace = await _projectStore.GetWorkspaceAsync(workspaceId, cancellationToken);
        if (workspace is null)
        {
            return;
        }

        bool updated = await _chatStore.UpdateWorkspaceAsync(
            chatId,
            workspace.ProjectId,
            workspace.Id,
            workspacePath,
            cancellationToken);

        if (!updated)
        {
            return;
        }

        session.ProjectId = workspace.ProjectId;
        session.WorkspaceId = workspace.Id;
        session.WorkspacePath = workspacePath;
        _sessions[chatId] = session;

        _logger.LogInformation(
            "Switched chat {ChatId} to worktree workspace {WorkspaceId} at {Path}",
            chatId,
            workspaceId,
            workspacePath);
    }

    private async Task<ScriptDispatchContext> BuildScriptDispatchContextAsync(
        ChatSession session,
        bool succeeded,
        CancellationToken cancellationToken)
    {
        string? branch = null;
        string? baseBranch = null;
        GitHostProviderSnapshot? gitHost = null;

        if (session.WorkspaceId is Guid workspaceId)
        {
            Entities.Workspace? workspace = await _projectStore.GetWorkspaceAsync(workspaceId, cancellationToken);
            branch = workspace?.Branch;
            baseBranch = workspace?.BaseBranch;
        }

        if (session.ProjectId is Guid projectId)
        {
            Entities.Project? project = await _projectStore.GetProjectAsync(projectId, cancellationToken);
            if (project is not null)
            {
                gitHost = new GitHostProviderSnapshot(
                    project.GitHostProvider,
                    project.DefaultBaseBranch,
                    project.DefaultWorktreeBranchPattern);
                baseBranch ??= project.DefaultBaseBranch;
            }
        }

        return new ScriptDispatchContext
        {
            ChatId = session.Id,
            Mode = session.Mode,
            Succeeded = succeeded,
            WorkspacePath = session.WorkspacePath,
            ProjectId = session.ProjectId,
            ParentChatId = session.ParentChatId,
            WorkspaceId = session.WorkspaceId,
            Branch = branch,
            BaseBranch = baseBranch,
            GitHost = gitHost
        };
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
