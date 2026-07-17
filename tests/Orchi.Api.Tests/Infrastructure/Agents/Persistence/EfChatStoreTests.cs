using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.Api.Infrastructure.Agents.Search;

namespace Orchi.Api.Tests.Infrastructure.Agents.Persistence;

public class EfChatStoreTests
{
    [Fact]
    public async Task ListAsync_OnEmptyDatabase_ReturnsEmpty()
    {
        await using StoreFixture fixture = await StoreFixture.CreateAsync();
        IReadOnlyList<ChatSession> sessions = await fixture.Store.ListAsync(CancellationToken.None);

        Assert.Empty(sessions);
    }

    [Fact]
    public async Task UpdateExternalSessionIdAsync_PersistsSessionId()
    {
        await using StoreFixture fixture = await StoreFixture.CreateAsync();
        var chatId = Guid.NewGuid();
        await fixture.Store.CreateAsync(
            new ChatCreateModel(chatId, "cursor", Directory.GetCurrentDirectory()),
            CancellationToken.None);

        await fixture.Store.UpdateExternalSessionIdAsync(chatId, "cursor-session-abc", CancellationToken.None);

        ChatSession? loaded = await fixture.Store.GetAsync(chatId, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("cursor-session-abc", loaded.ExternalSessionId);
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsRecentLimited()
    {
        await using StoreFixture fixture = await StoreFixture.CreateAsync();
        Guid olderId = Guid.NewGuid();
        Guid newerId = Guid.NewGuid();

        await fixture.Store.CreateAsync(
            new ChatCreateModel(olderId, "cursor", Directory.GetCurrentDirectory()),
            CancellationToken.None);
        await Task.Delay(20);
        await fixture.Store.CreateAsync(
            new ChatCreateModel(newerId, "cursor", Directory.GetCurrentDirectory()),
            CancellationToken.None);

        IReadOnlyList<ChatSession> results = await fixture.Store.SearchAsync(
            new ChatSearchCriteria(Limit: 1),
            CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(newerId, results[0].Id);
    }

    [Fact]
    public async Task SearchAsync_TextQuery_MatchesMessageContent()
    {
        await using StoreFixture fixture = await StoreFixture.CreateAsync();
        Guid matchId = Guid.NewGuid();
        Guid otherId = Guid.NewGuid();

        await fixture.Store.CreateAsync(
            new ChatCreateModel(matchId, "cursor", Directory.GetCurrentDirectory()),
            CancellationToken.None);
        await fixture.Store.CreateAsync(
            new ChatCreateModel(otherId, "cursor", Directory.GetCurrentDirectory()),
            CancellationToken.None);

        await fixture.Store.SaveUserMessageAsync(
            matchId,
            new ChatMessage(Guid.NewGuid(), "user", "find the unique-needle phrase", DateTimeOffset.UtcNow),
            CancellationToken.None);
        await fixture.Store.SaveUserMessageAsync(
            otherId,
            new ChatMessage(Guid.NewGuid(), "user", "unrelated content", DateTimeOffset.UtcNow),
            CancellationToken.None);

        IReadOnlyList<ChatSession> results = await fixture.Store.SearchAsync(
            new ChatSearchCriteria(Query: "unique-needle"),
            CancellationToken.None);

        Assert.Single(results);
        Assert.Equal(matchId, results[0].Id);
    }

    [Fact]
    public async Task SearchAsync_ExcludesSoftDeleted()
    {
        await using StoreFixture fixture = await StoreFixture.CreateAsync();
        var chatId = Guid.NewGuid();
        await fixture.Store.CreateAsync(
            new ChatCreateModel(chatId, "cursor", Directory.GetCurrentDirectory()),
            CancellationToken.None);
        await fixture.Store.SaveUserMessageAsync(
            chatId,
            new ChatMessage(Guid.NewGuid(), "user", "searchable-deleted", DateTimeOffset.UtcNow),
            CancellationToken.None);
        await fixture.Store.DeleteAsync(chatId, CancellationToken.None);

        IReadOnlyList<ChatSession> results = await fixture.Store.SearchAsync(
            new ChatSearchCriteria(Query: "searchable-deleted"),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task MarkReadAsync_WhenReadyForReview_SetsRead()
    {
        await using StoreFixture fixture = await StoreFixture.CreateAsync();
        var chatId = Guid.NewGuid();
        await fixture.Store.CreateAsync(
            new ChatCreateModel(chatId, "cursor", Directory.GetCurrentDirectory()),
            CancellationToken.None);
        await fixture.Store.UpdateStatusAsync(chatId, ChatStatus.ReadyForReview, CancellationToken.None);

        ChatSession? marked = await fixture.Store.MarkReadAsync(chatId, clearInProgress: true, CancellationToken.None);

        Assert.NotNull(marked);
        Assert.Equal(ChatStatus.Read, marked.Status);
        Assert.NotNull(marked.LastReadAt);
    }

    [Fact]
    public async Task MarkReadAsync_WhenInProgressAndClear_SetsRead()
    {
        await using StoreFixture fixture = await StoreFixture.CreateAsync();
        var chatId = Guid.NewGuid();
        await fixture.Store.CreateAsync(
            new ChatCreateModel(chatId, "cursor", Directory.GetCurrentDirectory()),
            CancellationToken.None);
        await fixture.Store.UpdateStatusAsync(chatId, ChatStatus.InProgress, CancellationToken.None);

        ChatSession? marked = await fixture.Store.MarkReadAsync(chatId, clearInProgress: true, CancellationToken.None);

        Assert.NotNull(marked);
        Assert.Equal(ChatStatus.Read, marked.Status);
        Assert.NotNull(marked.LastReadAt);
    }

    [Fact]
    public async Task MarkReadAsync_WhenInProgressAndKeep_KeepsInProgressAndSetsLastReadAt()
    {
        await using StoreFixture fixture = await StoreFixture.CreateAsync();
        var chatId = Guid.NewGuid();
        await fixture.Store.CreateAsync(
            new ChatCreateModel(chatId, "cursor", Directory.GetCurrentDirectory()),
            CancellationToken.None);
        await fixture.Store.UpdateStatusAsync(chatId, ChatStatus.InProgress, CancellationToken.None);

        ChatSession? marked = await fixture.Store.MarkReadAsync(chatId, clearInProgress: false, CancellationToken.None);

        Assert.NotNull(marked);
        Assert.Equal(ChatStatus.InProgress, marked.Status);
        Assert.NotNull(marked.LastReadAt);
    }

    [Fact]
    public async Task MarkReadAsync_DoesNotStompConcurrentReadyForReview()
    {
        await using StoreFixture fixture = await StoreFixture.CreateAsync();
        var chatId = Guid.NewGuid();
        await fixture.Store.CreateAsync(
            new ChatCreateModel(chatId, "cursor", Directory.GetCurrentDirectory()),
            CancellationToken.None);
        await fixture.Store.UpdateStatusAsync(chatId, ChatStatus.InProgress, CancellationToken.None);

        // Simulate the race: mark-read while in progress (keep), then turn completes to ready.
        Task<ChatSession?> markTask = fixture.Store.MarkReadAsync(
            chatId,
            clearInProgress: false,
            CancellationToken.None);
        await fixture.Store.UpdateStatusAsync(chatId, ChatStatus.ReadyForReview, CancellationToken.None);
        ChatSession? marked = await markTask;

        ChatSession? loaded = await fixture.Store.GetAsync(chatId, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.NotEqual(ChatStatus.InProgress, loaded.Status);
        Assert.True(
            loaded.Status is ChatStatus.ReadyForReview or ChatStatus.Read,
            $"Expected ReadyForReview or Read after race, got {loaded.Status}");
        Assert.NotNull(marked);
        Assert.NotNull(marked.LastReadAt);
    }

    private sealed class StoreFixture : IAsyncDisposable
    {
        private readonly string _databasePath;
        private readonly ServiceProvider _provider;

        private StoreFixture(string databasePath, ServiceProvider provider, EfChatStore store)
        {
            _databasePath = databasePath;
            _provider = provider;
            Store = store;
        }

        public EfChatStore Store { get; }

        public static async Task<StoreFixture> CreateAsync()
        {
            string databasePath = Path.Combine(Path.GetTempPath(), $"orchi-unit-{Guid.NewGuid():N}.db");
            var services = new ServiceCollection();
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));

            ServiceProvider provider = services.BuildServiceProvider();
            IDbContextFactory<AppDbContext> factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.MigrateAsync();
            }

            var composer = new ChatSearchComposer([new TextMatchChatSearchClause()]);
            var store = new EfChatStore(factory, composer);
            return new StoreFixture(databasePath, provider, store);
        }

        public async ValueTask DisposeAsync()
        {
            await _provider.DisposeAsync();
            SqliteConnection.ClearAllPools();

            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
    }
}
