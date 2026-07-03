using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Plans.Persistence;

namespace Orchi.Api.Tests.Infrastructure.Agents.Plans.Persistence;

public class EfPlanStoreTests
{
    [Fact]
    public async Task UpsertAsync_CreatesThenUpdatesPlan()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"orchi-plan-unit-{Guid.NewGuid():N}.db");
        Guid sourceChatId = Guid.NewGuid();

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
                db.Chats.Add(new Chat
                {
                    Id = sourceChatId,
                    AgentId = "cursor",
                    WorkspacePath = Directory.GetCurrentDirectory(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var store = new EfPlanStore(factory);

            await store.UpsertAsync(
                new PlanUpsertModel("auth-refactor", sourceChatId, "Auth refactor", "# Auth refactor\n\nInitial."),
                CancellationToken.None);

            await store.UpsertAsync(
                new PlanUpsertModel("auth-refactor", sourceChatId, "Auth refactor v2", "# Auth refactor\n\nUpdated."),
                CancellationToken.None);

            StoredPlan? loaded = await store.GetAsync(sourceChatId, "auth-refactor", CancellationToken.None);

            Assert.NotNull(loaded);
            Assert.Equal("Auth refactor v2", loaded.Title);
            Assert.Contains("Updated.", loaded.ContentMarkdown);

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                Assert.Equal(1, await db.Plans.CountAsync());
            }
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
    public async Task GetAsync_ReturnsNullForUnknownPlanOrSourceChat()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"orchi-plan-unit-{Guid.NewGuid():N}.db");
        Guid sourceChatId = Guid.NewGuid();

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
                db.Chats.Add(new Chat
                {
                    Id = sourceChatId,
                    AgentId = "cursor",
                    WorkspacePath = Directory.GetCurrentDirectory(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var store = new EfPlanStore(factory);

            await store.UpsertAsync(
                new PlanUpsertModel("auth-refactor", sourceChatId, "Auth refactor", "# Plan"),
                CancellationToken.None);

            Assert.Null(await store.GetAsync(sourceChatId, "missing-plan", CancellationToken.None));
            Assert.Null(await store.GetAsync(Guid.NewGuid(), "auth-refactor", CancellationToken.None));
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
