using Orchi.Api.Common.Results;
using Orchi.Api.Entities;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Persistence;

namespace Orchi.Api.Infrastructure.Agents;

public interface IChatStatusService
{
    Task SetInProgressAsync(Guid chatId, CancellationToken cancellationToken);

    Task SetReadyForReviewAsync(Guid chatId, CancellationToken cancellationToken);

    Task<Result<ChatSession>> MarkReadAsync(Guid chatId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatStatusSnapshotItem>> ListStatusesAsync(CancellationToken cancellationToken);
}

public sealed class ChatStatusService(
    IChatStore chatStore,
    ChatStatusEventHub eventHub) : IChatStatusService
{
    public Task SetInProgressAsync(Guid chatId, CancellationToken cancellationToken) =>
        SetStatusAsync(chatId, ChatStatus.InProgress, cancellationToken);

    public Task SetReadyForReviewAsync(Guid chatId, CancellationToken cancellationToken) =>
        SetStatusAsync(chatId, ChatStatus.ReadyForReview, cancellationToken);

    public async Task<Result<ChatSession>> MarkReadAsync(
        Guid chatId,
        CancellationToken cancellationToken)
    {
        ChatSession? session = await chatStore.MarkReadAsync(chatId, cancellationToken);
        if (session is null)
        {
            return Result.Failure<ChatSession>(
                Error.NotFound($"Chat '{chatId}' was not found."));
        }

        await eventHub.PublishAsync(
            new ChatStatusChangedEvent(session.Id, session.Status),
            cancellationToken);

        return Result.Success(session);
    }

    public async Task<IReadOnlyList<ChatStatusSnapshotItem>> ListStatusesAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatSession> sessions = await chatStore.ListAsync(cancellationToken);
        return sessions
            .Select(session => new ChatStatusSnapshotItem(session.Id, session.Status))
            .ToArray();
    }

    private async Task SetStatusAsync(
        Guid chatId,
        ChatStatus status,
        CancellationToken cancellationToken)
    {
        ChatStatus? applied = await chatStore.UpdateStatusAsync(chatId, status, cancellationToken);
        if (applied is null)
        {
            return;
        }

        await eventHub.PublishAsync(new ChatStatusChangedEvent(chatId, applied.Value), cancellationToken);
    }
}
