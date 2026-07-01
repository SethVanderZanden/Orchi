using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orchi.Api.Data;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Persistence;

namespace Orchi.Api.Tests.Infrastructure.Agents.Persistence;

public class EfChatStoreTests
{
    [Fact]
    public async Task ListAsync_OnEmptyDatabase_ReturnsEmpty()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"orchi-unit-{Guid.NewGuid():N}.db");

        try
        {
            var services = new ServiceCollection();
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));

            await using ServiceProvider provider = services.BuildServiceProvider();
            IDbContextFactory<AppDbContext> factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.MigrateAsync();
            }

            var store = new EfChatStore(factory);
            IReadOnlyList<Orchi.Api.Infrastructure.Agents.ChatSession> sessions =
                await store.ListAsync(CancellationToken.None);

            Assert.Empty(sessions);

            await provider.DisposeAsync();
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task UpdateExternalSessionIdAsync_PersistsSessionId()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"orchi-unit-{Guid.NewGuid():N}.db");

        try
        {
            var services = new ServiceCollection();
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));

            await using ServiceProvider provider = services.BuildServiceProvider();
            IDbContextFactory<AppDbContext> factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.MigrateAsync();
            }

            var store = new EfChatStore(factory);
            var chatId = Guid.NewGuid();
            await store.CreateAsync(
                new ChatCreateModel(chatId, "cursor", Directory.GetCurrentDirectory(), ChatMode.Agent, null, null),
                CancellationToken.None);

            await store.UpdateExternalSessionIdAsync(chatId, "cursor-session-abc", CancellationToken.None);

            Orchi.Api.Infrastructure.Agents.ChatSession? loaded = await store.GetAsync(chatId, CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal("cursor-session-abc", loaded.ExternalSessionId);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task UpdateModeAsync_ClearsExternalSessionId()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"orchi-unit-{Guid.NewGuid():N}.db");

        try
        {
            var services = new ServiceCollection();
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));

            await using ServiceProvider provider = services.BuildServiceProvider();
            IDbContextFactory<AppDbContext> factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.MigrateAsync();
            }

            var store = new EfChatStore(factory);
            var chatId = Guid.NewGuid();
            await store.CreateAsync(
                new ChatCreateModel(chatId, "cursor", Directory.GetCurrentDirectory(), ChatMode.Agent, null, null),
                CancellationToken.None);
            await store.UpdateExternalSessionIdAsync(chatId, "cursor-session-abc", CancellationToken.None);

            await store.UpdateModeAsync(chatId, ChatMode.Plan, null, CancellationToken.None);

            Orchi.Api.Infrastructure.Agents.ChatSession? loaded = await store.GetAsync(chatId, CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Null(loaded.ExternalSessionId);
            Assert.Equal(ChatMode.Plan, loaded.Mode);
        }
        finally
        {
            SqliteConnection.ClearAllPools();

            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}
