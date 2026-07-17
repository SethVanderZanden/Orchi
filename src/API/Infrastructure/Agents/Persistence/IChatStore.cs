using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Search;

namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed record ChatCreateModel(
    Guid Id,
    string AgentId,
    string WorkspacePath,
    string Mode = "default",
    Guid? ParentChatId = null,
    string? PlanFilePath = null,
    Guid? ProjectId = null,
    Guid? WorkspaceId = null,
    string? ModelId = null,
    string? ContextSizeId = null,
    string? ReasoningEffortId = null,
    string? ApprovalPolicyId = null);

public interface IChatStore
{
    Task<ChatSession> CreateAsync(ChatCreateModel model, CancellationToken cancellationToken);

    Task<ChatSession?> GetAsync(Guid chatId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid chatId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatSession>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatSession>> SearchAsync(
        ChatSearchCriteria criteria,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatSession>> ListChildrenAsync(Guid parentChatId, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid chatId, CancellationToken cancellationToken);

    Task SaveUserMessageAsync(Guid chatId, ChatMessage message, CancellationToken cancellationToken);

    Task SaveAssistantMessageAsync(
        Guid chatId,
        ChatMessage message,
        string? externalSessionId,
        CancellationToken cancellationToken);

    Task SaveStatusMessageAsync(Guid chatId, ChatMessage message, CancellationToken cancellationToken);

    Task UpdateExternalSessionIdAsync(
        Guid chatId,
        string externalSessionId,
        CancellationToken cancellationToken);

    Task<bool> UpdateModeAsync(Guid chatId, string mode, CancellationToken cancellationToken);

    Task<bool> UpdateModelIdAsync(Guid chatId, string? modelId, CancellationToken cancellationToken);

    Task<bool> UpdateContextSizeIdAsync(Guid chatId, string? contextSizeId, CancellationToken cancellationToken);

    Task<bool> UpdateReasoningEffortIdAsync(
        Guid chatId,
        string? reasoningEffortId,
        CancellationToken cancellationToken);

    Task<bool> UpdateApprovalPolicyIdAsync(
        Guid chatId,
        string? approvalPolicyId,
        CancellationToken cancellationToken);

    Task<bool> UpdateRuntimeAsync(
        Guid chatId,
        string agentId,
        string mode,
        string? modelId,
        string? contextSizeId,
        string? reasoningEffortId,
        string? approvalPolicyId,
        bool clearExternalSessionId,
        CancellationToken cancellationToken);

    Task<ChatStatus?> UpdateStatusAsync(Guid chatId, ChatStatus status, CancellationToken cancellationToken);

    /// <param name="clearInProgress">
    /// When true, always set status to <see cref="ChatStatus.Read"/>.
    /// When false, keep <see cref="ChatStatus.InProgress"/> if that is the current status.
    /// </param>
    Task<ChatSession?> MarkReadAsync(Guid chatId, bool clearInProgress, CancellationToken cancellationToken);
}
