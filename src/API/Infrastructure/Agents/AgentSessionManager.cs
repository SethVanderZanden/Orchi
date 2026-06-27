using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Orchi.Api.Common.Results;

namespace Orchi.Api.Infrastructure.Agents;

public sealed class AgentSessionManager
{
    private readonly ConcurrentDictionary<Guid, ChatSession> _sessions = new();
    private readonly IAgentAdapterFactory _adapterFactory;
    private readonly ILogger<AgentSessionManager> _logger;

    public AgentSessionManager(IAgentAdapterFactory adapterFactory, ILogger<AgentSessionManager> logger)
    {
        _adapterFactory = adapterFactory;
        _logger = logger;
    }

    public IReadOnlyList<ChatSession> ListSessions() =>
        _sessions.Values
            .OrderByDescending(session => session.Messages.LastOrDefault()?.CreatedAt ?? DateTimeOffset.MinValue)
            .ToList();

    public ChatSession? GetSession(Guid chatId) =>
        _sessions.TryGetValue(chatId, out ChatSession? session) ? session : null;

    public Result<ChatSession> CreateSession(string agentId, string workspacePath)
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

        var session = new ChatSession
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            WorkspacePath = fullPath
        };

        _sessions[session.Id] = session;
        return Result.Success(session);
    }

    public Result CloseSession(Guid chatId)
    {
        if (!_sessions.TryRemove(chatId, out ChatSession? session))
        {
            return Result.Failure(Error.NotFound($"Chat '{chatId}' was not found."));
        }

        StopRunningProcess(session);
        return Result.Success();
    }

    public void CloseAllSessions()
    {
        foreach (Guid chatId in _sessions.Keys.ToList())
        {
            CloseSession(chatId);
        }
    }

    public ChatMessage AppendUserMessage(Guid chatId, string content)
    {
        ChatSession session = GetRequiredSession(chatId);
        var message = new ChatMessage(
            Guid.NewGuid(),
            "user",
            content,
            DateTimeOffset.UtcNow);

        lock (session.Sync)
        {
            session.Messages.Add(message);
        }

        return message;
    }

    public async IAsyncEnumerable<AgentEvent> SendMessageAsync(
        Guid chatId,
        string content,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ChatSession session = GetRequiredSession(chatId);
        IAgentAdapter adapter = _adapterFactory.GetAdapter(session.AgentId);

        StopRunningProcess(session);

        AppendUserMessage(chatId, content);

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

        await foreach (AgentEvent agentEvent in adapter.SendMessageAsync(session, content, runCts.Token))
        {
            switch (agentEvent)
            {
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
                    }

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

                    yield return error;
                    break;

                default:
                    yield return agentEvent;
                    break;
            }
        }

        session.RunCts = null;
    }

    private ChatSession GetRequiredSession(Guid chatId) =>
        GetSession(chatId) ?? throw new KeyNotFoundException($"Chat '{chatId}' was not found.");

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
