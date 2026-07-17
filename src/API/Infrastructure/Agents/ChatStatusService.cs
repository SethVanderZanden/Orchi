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

    /// <summary>
    /// Updates <see cref="ChatSession.LastReadAt"/> without clearing <see cref="ChatStatus.InProgress"/>.
    /// Used while an agent turn is still running.
    /// </summary>
    Task<Result<ChatSession>> TouchLastReadAsync(Guid chatId, CancellationToken cancellationToken);

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

    public Task<Result<ChatSession>> MarkReadAsync(
        Guid chatId,
        CancellationToken cancellationToken) =>
        ApplyReadAsync(chatId, clearInProgress: true, cancellationToken);

    public Task<Result<ChatSession>> TouchLastReadAsync(
        Guid chatId,
        CancellationToken cancellationToken) =>
        ApplyReadAsync(chatId, clearInProgress: false, cancellationToken);

    public async Task<IReadOnlyList<ChatStatusSnapshotItem>> ListStatusesAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatSession> sessions = await chatStore.ListAsync(cancellationToken);
        return sessions
            .Select(session => new ChatStatusSnapshotItem(session.Id, session.Status))
            .ToArray();
    }

    private async Task<Result<ChatSession>> ApplyReadAsync(
        Guid chatId,
        bool clearInProgress,
        CancellationToken cancellationToken)
    {
        ChatSession? before = await chatStore.GetAsync(chatId, cancellationToken);
        if (before is null)
        {
            return Result.Failure<ChatSession>(
                Error.NotFound($"Chat '{chatId}' was not found."));
        }

        ChatStatus previousStatus = before.Status;
        ChatSession? session = await chatStore.MarkReadAsync(chatId, clearInProgress, cancellationToken);
        if (session is null)
        {
            return Result.Failure<ChatSession>(
                Error.NotFound($"Chat '{chatId}' was not found."));
        }

        // Avoid re-broadcasting InProgress from touch-while-running; that can
        // clobber a concurrent ReadyForReview event on the client.
        if (session.Status != previousStatus)
        {
            await eventHub.PublishAsync(
                new ChatStatusChangedEvent(session.Id, session.Status),
                cancellationToken);
        }

        return Result.Success(session);
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
