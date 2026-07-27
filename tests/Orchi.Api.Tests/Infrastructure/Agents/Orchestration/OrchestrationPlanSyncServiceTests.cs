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
    public async Task SyncFromMessagesAsync_WritesPlanFilesAndSequenceFile()
    {
        string workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-plan-sync-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspacePath);
        Guid sourceChatId = Guid.NewGuid();

        try
        {
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
                ProjectId = Guid.NewGuid(),
                Messages =
                {
                    new ChatMessage(
                        Guid.NewGuid(),
                        "assistant",
                        """
                        <!-- orchi-plan:auth-refactor -->
                        # Auth refactor

                        Implement JWT auth.
                        <!-- /orchi-plan -->

                        <!-- orchi-plan-sequence -->
                        auth-refactor
                        <!-- /orchi-plan-sequence -->
                        """,
                        DateTimeOffset.UtcNow,
                        Status: "complete")
                }
            };

            await syncService.SyncFromMessagesAsync(parent, CancellationToken.None);

            string planFile = Path.Combine(workspacePath, ".orchi", "plan-auth-refactor.md");
            Assert.True(File.Exists(planFile));
            Assert.Contains("Implement JWT auth.", await File.ReadAllTextAsync(planFile));

            string sequenceFile = Path.Combine(workspacePath, ".orchi", "plan-sequence.txt");
            Assert.True(File.Exists(sequenceFile));
            Assert.Equal("auth-refactor", (await File.ReadAllTextAsync(sequenceFile)).Trim());

            StoredPlan? stored = await planStore.GetAsync(sourceChatId, "auth-refactor", CancellationToken.None);
            Assert.NotNull(stored);
            Assert.Equal("Auth refactor", stored.Title);
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
