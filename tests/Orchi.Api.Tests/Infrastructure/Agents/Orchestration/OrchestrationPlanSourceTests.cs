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

public class OrchestrationPlanSourceTests
{
    [Fact]
    public async Task ResolvePlanContentAsync_PrefersStoredFileOverMessageFallback()
    {
        string workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-plan-source-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspacePath);
        Guid sourceChatId = Guid.NewGuid();
        string databasePath = Path.Combine(Path.GetTempPath(), $"orchi-plan-source-db-{Guid.NewGuid():N}.db");

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
                    Mode = OrchestrationAgentModeStrategy.Mode,
                    WorkspacePath = workspacePath,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var planStore = new EfPlanStore(factory);
            var fileStore = new OrchiArtifactFileStore();
            var planSource = new OrchestrationPlanSource(planStore, fileStore);

            await planStore.UpsertAsync(
                new PlanUpsertModel(
                    "auth-refactor",
                    sourceChatId,
                    "Auth refactor",
                    "# Auth refactor\n\nStored content."),
                CancellationToken.None);

            await fileStore.WriteAsync(
                workspacePath,
                ".orchi/plan-auth-refactor.md",
                "# Auth refactor\n\nFile content.",
                CancellationToken.None);

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

                        Message content.
                        <!-- /orchi-plan -->
                        """,
                        DateTimeOffset.UtcNow,
                        Status: "complete")
                }
            };

            string? resolved = await planSource.ResolvePlanContentAsync(
                parent,
                "auth-refactor",
                "Client fallback content.",
                CancellationToken.None);

            Assert.NotNull(resolved);
            Assert.Contains("File content.", resolved);
        }
        finally
        {
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }

            SqliteConnection.ClearAllPools();

            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}
