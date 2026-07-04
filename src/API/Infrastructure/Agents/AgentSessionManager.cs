using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Models;
using Orchi.Api.Infrastructure.Projects;

namespace Orchi.Api.Infrastructure.Agents;

public sealed class AgentSessionManager
{
    private readonly ConcurrentDictionary<Guid, ChatSession> _sessions = new();
    private readonly IChatStore _chatStore;
    private readonly IProjectStore _projectStore;
    private readonly IAgentAdapterFactory _adapterFactory;
    private readonly IAgentModeStrategyFactory _modeStrategyFactory;
    private readonly IAgentModelCatalogService _modelCatalogService;
    private readonly IAgentModeModelDefaultService _modeDefaultService;
    private readonly IAgentPromptComposer _promptComposer;
    private readonly ILogger<AgentSessionManager> _logger;

    public AgentSessionManager(
        IChatStore chatStore,
        IProjectStore projectStore,
        IAgentAdapterFactory adapterFactory,
        IAgentModeStrategyFactory modeStrategyFactory,
        IAgentModelCatalogService modelCatalogService,
        IAgentModeModelDefaultService modeDefaultService,
        IAgentPromptComposer promptComposer,
        ILogger<AgentSessionManager> logger)
    {
        _chatStore = chatStore;
        _projectStore = projectStore;
        _adapterFactory = adapterFactory;
        _modeStrategyFactory = modeStrategyFactory;
        _modelCatalogService = modelCatalogService;
        _modeDefaultService = modeDefaultService;
        _promptComposer = promptComposer;
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
        ChatSession? loaded = await _chatStore.GetAsync(chatId, cancellationToken);
        if (loaded is null)
        {
            _sessions.TryRemove(chatId, out _);
            return null;
        }

        if (_sessions.TryGetValue(chatId, out ChatSession? cached))
        {
            cached.ProjectId = loaded.ProjectId;
            cached.WorkspaceId = loaded.WorkspaceId;
            cached.ModelId = loaded.ModelId;
            return cached;
        }

        _sessions[chatId] = loaded;
        return loaded;
    }

    public async Task<Result<ChatSession>> CreateSessionAsync(
        string agentId,
        Guid workspaceId,
        string? mode = null,
        Guid? parentChatId = null,
        string? planFilePath = null,
        string? modelId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return Result.Failure<ChatSession>(Error.Validation("Agent.Required", "Agent is required."));
        }

        if (workspaceId == Guid.Empty)
        {
            return Result.Failure<ChatSession>(Error.Validation("Workspace.Required", "Workspace id is required."));
        }

        string resolvedMode = string.IsNullOrWhiteSpace(mode) ? DefaultAgentModeStrategy.Mode : mode.Trim();
        string? resolvedModelId = string.IsNullOrWhiteSpace(modelId) ? null : modelId.Trim();

        try
        {
            _ = _modeStrategyFactory.GetStrategy(resolvedMode);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ChatSession>(Error.Validation("Mode.Unsupported", ex.Message));
        }

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
            _ = _adapterFactory.GetAdapter(agentId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<ChatSession>(Error.Validation("Agent.Unsupported", ex.Message));
        }

        if (resolvedModelId is null)
        {
            resolvedModelId = await _modeDefaultService.ResolveAsync(agentId, resolvedMode, cancellationToken);
        }

        if (resolvedModelId is not null)
        {
            bool enabled = await _modelCatalogService.IsEnabledModelAsync(agentId, resolvedModelId, cancellationToken);
            if (!enabled)
            {
                return Result.Failure<ChatSession>(
                    Error.Validation("Model.Unsupported", $"Model '{resolvedModelId}' is not available."));
            }
        }

        var sessionId = Guid.NewGuid();
        ChatSession session = await _chatStore.CreateAsync(
            new ChatCreateModel(
                sessionId,
                agentId,
                fullPath,
                resolvedMode,
                parentChatId,
                planFilePath,
                workspace.ProjectId,
                workspace.Id,
                resolvedModelId),
            cancellationToken);

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

        bool updated = await _chatStore.UpdateModeAsync(chatId, resolvedMode, cancellationToken);
        if (!updated)
        {
            return Result.Failure<ChatSession>(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        session.Mode = resolvedMode;
        _sessions[chatId] = session;
        return Result.Success(session);
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

    private IReadOnlyList<string> BuildCliArgs(ChatSession session)
    {
        var args = new List<string>(_promptComposer.GetExtraCliArgs(session.Mode));

        if (!string.IsNullOrWhiteSpace(session.ModelId))
        {
            args.Add("--model");
            args.Add(session.ModelId);
        }

        return args;
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
