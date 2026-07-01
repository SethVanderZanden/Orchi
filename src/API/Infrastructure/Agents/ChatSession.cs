using System.Diagnostics;
using Orchi.Api.Infrastructure.Agents.Modes;

namespace Orchi.Api.Infrastructure.Agents;

public sealed class ChatSession
{
    public required Guid Id { get; init; }

    public required string AgentId { get; init; }

    public required string WorkspacePath { get; init; }

    public required ChatMode Mode { get; set; }

    public string? PreviousModeKey { get; set; }

    public DateTimeOffset? ModeChangedAt { get; set; }

    public Guid? ParentChatId { get; init; }

    public Guid? AttachedPlanId { get; set; }

    public Guid? GoalChatId { get; set; }

    public string? ExternalSessionId { get; set; }

    public List<ChatMessage> Messages { get; } = [];

    public List<string> GoalJournal { get; } = [];

    public Process? RunningProcess { get; set; }

    public CancellationTokenSource? RunCts { get; set; }

    /// <summary>Per-session lock object; lock on this when reading or mutating Messages, RunCts, or RunningProcess.</summary>
    public object Sync { get; } = new();
}
