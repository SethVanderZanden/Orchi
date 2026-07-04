using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Plans.Persistence;
using Orchi.Api.Infrastructure.Caching;

namespace Orchi.Api.Tests.Infrastructure.Agents.Plans.Persistence;

public class CachingPlanStoreTests
{
    [Fact]
    public async Task GetAsync_AfterUpsert_ReturnsFreshContent()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"orchi-plan-cache-{Guid.NewGuid():N}.db");
        Guid sourceChatId = Guid.NewGuid();

        try
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cache:DefaultExpirationMinutes"] = "5",
                    ["Cache:PlanExpirationMinutes"] = "10",
                    ["Cache:Distributed:Enabled"] = "false"
                })
                .Build();

            services.AddOrchiCaching(configuration);
            services.AddSingleton<EfPlanStore>();
            services.AddSingleton<IPlanStore, CachingPlanStore>();

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

            IPlanStore store = provider.GetRequiredService<IPlanStore>();

            await store.UpsertAsync(
                new PlanUpsertModel("auth-refactor", sourceChatId, "Auth refactor", "# Initial"),
                CancellationToken.None);

            StoredPlan? cachedRead = await store.GetAsync(sourceChatId, "auth-refactor", CancellationToken.None);
            Assert.NotNull(cachedRead);
            Assert.Equal("Auth refactor", cachedRead.Title);

            await store.UpsertAsync(
                new PlanUpsertModel("auth-refactor", sourceChatId, "Auth refactor v2", "# Updated"),
                CancellationToken.None);

            StoredPlan? freshRead = await store.GetAsync(sourceChatId, "auth-refactor", CancellationToken.None);

            Assert.NotNull(freshRead);
            Assert.Equal("Auth refactor v2", freshRead.Title);
            Assert.Contains("Updated", freshRead.ContentMarkdown);
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
