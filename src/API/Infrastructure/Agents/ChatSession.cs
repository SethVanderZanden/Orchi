using System.Diagnostics;

namespace Orchi.Api.Infrastructure.Agents;

public sealed class ChatSession
{
    public required Guid Id { get; init; }

    public required string AgentId { get; init; }

    public required string WorkspacePath { get; init; }

    public string? ExternalSessionId { get; set; }

    public List<ChatMessage> Messages { get; } = [];

    public Process? RunningProcess { get; set; }

    public CancellationTokenSource? RunCts { get; set; }

    public object Sync { get; } = new();
}
