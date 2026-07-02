using Orchi.Api.Infrastructure.Agents;

namespace Orchi.Api.Infrastructure.Agents.Persistence;

public sealed record ChatCreateModel(
    Guid Id,
    string AgentId,
    string WorkspacePath);

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
}
