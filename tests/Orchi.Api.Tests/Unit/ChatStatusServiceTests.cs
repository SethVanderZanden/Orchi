using Orchi.Api.Entities;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Persistence;

namespace Orchi.Api.Tests.Unit;

public class ChatStatusServiceTests
{
    [Fact]
    public async Task MarkRead_WhenMissing_ReturnsNotFound()
    {
        var store = new FakeChatStore();
        var hub = new ChatStatusEventHub();
        var service = new ChatStatusService(store, hub);

        var result = await service.MarkReadAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task SetInProgress_PublishesStatusEvent()
    {
        var chatId = Guid.NewGuid();
        var store = new FakeChatStore();
        store.Sessions[chatId] = new ChatSession
        {
            Id = chatId,
            AgentId = "cursor",
            WorkspacePath = "/tmp",
            Status = ChatStatus.Read
        };

        var hub = new ChatStatusEventHub();
        (Guid subscriptionId, var reader) = hub.Subscribe();
        var service = new ChatStatusService(store, hub);

        await service.SetInProgressAsync(chatId, CancellationToken.None);

        Assert.True(reader.TryRead(out ChatStatusChangedEvent? statusEvent));
        Assert.NotNull(statusEvent);
        Assert.Equal(chatId, statusEvent.ChatId);
        Assert.Equal(ChatStatus.InProgress, statusEvent.Status);
        Assert.Equal(ChatStatus.InProgress, store.Sessions[chatId].Status);

        hub.Unsubscribe(subscriptionId);
    }

    [Fact]
    public async Task ListStatuses_ReturnsSnapshotItems()
    {
        var chatId = Guid.NewGuid();
        var store = new FakeChatStore();
        store.Sessions[chatId] = new ChatSession
        {
            Id = chatId,
            AgentId = "cursor",
            WorkspacePath = "/tmp",
            Status = ChatStatus.ReadyForReview
        };

        var service = new ChatStatusService(store, new ChatStatusEventHub());
        IReadOnlyList<ChatStatusSnapshotItem> snapshot =
            await service.ListStatusesAsync(CancellationToken.None);

        Assert.Contains(snapshot, item => item.ChatId == chatId && item.Status == ChatStatus.ReadyForReview);
    }

    private sealed class FakeChatStore : IChatStore
    {
        public Dictionary<Guid, ChatSession> Sessions { get; } = new();

        public Task<ChatSession> CreateAsync(ChatCreateModel model, CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<ChatSession?> GetAsync(Guid chatId, CancellationToken cancellationToken) =>
            Task.FromResult(Sessions.TryGetValue(chatId, out ChatSession? session) ? session : null);

        public Task<bool> ExistsAsync(Guid chatId, CancellationToken cancellationToken) =>
            Task.FromResult(Sessions.ContainsKey(chatId));

        public Task<IReadOnlyList<ChatSession>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ChatSession>>(Sessions.Values.ToArray());

        public Task<IReadOnlyList<ChatSession>> SearchAsync(
            Orchi.Api.Infrastructure.Agents.Search.ChatSearchCriteria criteria,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ChatSession>>(
                Sessions.Values
                    .OrderByDescending(session =>
                        session.Messages.LastOrDefault()?.CreatedAt ?? DateTimeOffset.MinValue)
                    .Take(criteria.ResolveLimit())
                    .ToArray());

        public Task<IReadOnlyList<ChatSession>> ListChildrenAsync(
            Guid parentChatId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ChatSession>>(
                Sessions.Values.Where(session => session.ParentChatId == parentChatId).ToArray());

        public Task<bool> DeleteAsync(Guid chatId, CancellationToken cancellationToken) =>
            Task.FromResult(Sessions.Remove(chatId));

        public Task SaveUserMessageAsync(
            Guid chatId,
            ChatMessage message,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task SaveAssistantMessageAsync(
            Guid chatId,
            ChatMessage message,
            string? externalSessionId,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task SaveStatusMessageAsync(
            Guid chatId,
            ChatMessage message,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task UpdateExternalSessionIdAsync(
            Guid chatId,
            string externalSessionId,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> UpdateModeAsync(Guid chatId, string mode, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> UpdateModelIdAsync(Guid chatId, string? modelId, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> UpdateContextSizeIdAsync(
            Guid chatId,
            string? contextSizeId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> UpdateReasoningEffortIdAsync(
            Guid chatId,
            string? reasoningEffortId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> UpdateApprovalPolicyIdAsync(
            Guid chatId,
            string? approvalPolicyId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> UpdateRuntimeAsync(
            Guid chatId,
            string agentId,
            string mode,
            string? modelId,
            string? contextSizeId,
            string? reasoningEffortId,
            string? approvalPolicyId,
            bool clearExternalSessionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<ChatStatus?> UpdateStatusAsync(
            Guid chatId,
            ChatStatus status,
            CancellationToken cancellationToken)
        {
            if (!Sessions.TryGetValue(chatId, out ChatSession? session))
            {
                return Task.FromResult<ChatStatus?>(null);
            }

            session.Status = status;
            return Task.FromResult<ChatStatus?>(status);
        }

        public Task<ChatSession?> MarkReadAsync(
            Guid chatId,
            bool clearInProgress,
            CancellationToken cancellationToken)
        {
            if (!Sessions.TryGetValue(chatId, out ChatSession? session))
            {
                return Task.FromResult<ChatSession?>(null);
            }

            session.LastReadAt = DateTimeOffset.UtcNow;
            if (clearInProgress || session.Status != ChatStatus.InProgress)
            {
                session.Status = ChatStatus.Read;
            }

            return Task.FromResult<ChatSession?>(session);
        }
    }
}
