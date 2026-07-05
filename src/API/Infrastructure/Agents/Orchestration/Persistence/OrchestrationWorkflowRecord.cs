namespace Orchi.Api.Infrastructure.Agents.Orchestration.Persistence;

public sealed record OrchestrationWorkflowRecord(
    Guid ParentChatId,
    string Status,
    IReadOnlyList<string> SequencePlanIds,
    int NextSequenceIndex,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
