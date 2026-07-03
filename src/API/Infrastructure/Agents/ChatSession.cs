using System.Diagnostics;

namespace Orchi.Api.Infrastructure.Agents;

public sealed class ChatSession
{
    public required Guid Id { get; init; }

    public required string AgentId { get; init; }

    public required string WorkspacePath { get; init; }

    public string Mode { get; init; } = "default";

    public Guid? ParentChatId { get; init; }

    public string? PlanFilePath { get; init; }

    public string? ExternalSessionId { get; set; }

    public List<ChatMessage> Messages { get; } = [];

    public Process? RunningProcess { get; set; }

    public CancellationTokenSource? RunCts { get; set; }

    /// <summary>Per-session lock object; lock on this when reading or mutating Messages, RunCts, or RunningProcess.</summary>
    public object Sync { get; } = new();
}
