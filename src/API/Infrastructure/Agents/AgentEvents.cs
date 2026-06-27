namespace Orchi.Api.Infrastructure.Agents;

public abstract record AgentEvent;

public sealed record AgentStatusEvent(string Phase) : AgentEvent;

public sealed record AgentTextDeltaEvent(string Text) : AgentEvent;

public sealed record AgentToolEvent(string Name, string Status, string? Detail) : AgentEvent;

public sealed record AgentCompletedEvent(string? ExternalSessionId, string FullText) : AgentEvent;

public sealed record AgentErrorEvent(string Code, string Message) : AgentEvent;
