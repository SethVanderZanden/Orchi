using Orchi.Api.Infrastructure.Agents;

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
    string? ModelId = null);

public interface IChatStore
{
    Task<ChatSession> CreateAsync(ChatCreateModel model, CancellationToken cancellationToken);

    Task<ChatSession?> GetAsync(Guid chatId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid chatId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatSession>> ListAsync(CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid chatId, CancellationToken cancellationToken);

    Task SaveUserMessageAsync(Guid chatId, ChatMessage message, CancellationToken cancellationToken);

    Task SaveAssistantMessageAsync(
        Guid chatId,
        ChatMessage message,
        string? externalSessionId,
        CancellationToken cancellationToken);

    Task UpdateExternalSessionIdAsync(
        Guid chatId,
        string externalSessionId,
        CancellationToken cancellationToken);

    Task<bool> UpdateModeAsync(Guid chatId, string mode, CancellationToken cancellationToken);

    Task<bool> UpdateModelIdAsync(Guid chatId, string? modelId, CancellationToken cancellationToken);
}
