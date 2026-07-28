using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Orchestration;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;
using Orchi.Api.Infrastructure.Agents.Plans.Persistence;

namespace Orchi.Api.Tests.Infrastructure.Agents.Orchestration;

public class OrchestrationPlanSyncServiceTests
{
    [Fact]
    public async Task SyncFromWorkspaceAsync_UpsertsDiscoveredPlanFilesWithoutMessageReferences()
    {
        string workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-plan-sync-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(workspacePath, ".orchi"));
        Guid sourceChatId = Guid.NewGuid();

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(workspacePath, ".orchi", "plan-auth-refactor.md"),
                """
                # Auth refactor

                Implement JWT auth.
                """);

            await File.WriteAllTextAsync(
                Path.Combine(workspacePath, ".orchi", "plan-sequence.txt"),
                "auth-refactor");

            var services = new ServiceCollection();
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite($"Data Source={Path.Combine(Path.GetTempPath(), $"orchi-plan-sync-db-{Guid.NewGuid():N}.db")}"));

            await using ServiceProvider provider = services.BuildServiceProvider();
            IDbContextFactory<AppDbContext> factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.MigrateAsync();
                db.Chats.Add(new Chat
                {
                    Id = sourceChatId,
                    AgentId = "cursor",
                    Mode = OrchestrationAgentModeStrategy.Mode,
                    WorkspacePath = workspacePath,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var planStore = new EfPlanStore(factory);
            var fileStore = new OrchiArtifactFileStore();
            var writerFactory = new OrchiArtifactWriterFactory([
                new ImplementationPlanWriterStrategy(fileStore)
            ]);

            var syncService = new OrchestrationPlanSyncService(
                planStore,
                writerFactory,
                fileStore,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<OrchestrationPlanSyncService>.Instance);

            var parent = new ChatSession
            {
                Id = sourceChatId,
                WorkspacePath = workspacePath,
                Mode = OrchestrationAgentModeStrategy.Mode,
                AgentId = "cursor",
                WorkspaceId = Guid.NewGuid(),
                ProjectId = Guid.NewGuid()
            };

            await syncService.SyncFromWorkspaceAsync(parent, CancellationToken.None);

            StoredPlan? stored = await planStore.GetAsync(sourceChatId, "auth-refactor", CancellationToken.None);
            Assert.NotNull(stored);
            Assert.Equal("Auth refactor", stored.Title);
            Assert.Contains("Implement JWT auth", stored.ContentMarkdown);
        }
        finally
        {
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }

            SqliteConnection.ClearAllPools();
        }
    }
}
