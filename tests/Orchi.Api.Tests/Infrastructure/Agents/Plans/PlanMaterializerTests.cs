using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Plans;
using Orchi.Api.Infrastructure.Agents.Plans.Artifacts;
using Orchi.Api.Infrastructure.Agents.Plans.Persistence;

namespace Orchi.Api.Tests.Infrastructure.Agents.Plans;

public class PlanMaterializerTests
{
    [Fact]
    public async Task MaterializeAsync_WritesPlanFilesAndUpsertsStoreFromLatestMessage()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"orchi-plan-mat-{Guid.NewGuid():N}.db");
        string workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-plan-ws-{Guid.NewGuid():N}");
        Guid sourceChatId = Guid.NewGuid();
        Directory.CreateDirectory(workspacePath);

        try
        {
            await using ServiceProvider provider = BuildProvider(databasePath);
            IDbContextFactory<AppDbContext> factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.MigrateAsync();
                db.Chats.Add(new Chat
                {
                    Id = sourceChatId,
                    AgentId = "cursor",
                    WorkspacePath = workspacePath,
                    Mode = "orchestration",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var session = new ChatSession
            {
                Id = sourceChatId,
                AgentId = "cursor",
                WorkspacePath = workspacePath,
                Mode = "orchestration"
            };
            session.Messages.Add(
                new ChatMessage(
                    Guid.NewGuid(),
                    "assistant",
                    """
                    <!-- orchi-plan:auth-refactor -->
                    # Auth refactor

                    ## Summary
                    Add JWT auth.
                    <!-- /orchi-plan -->
                    """,
                    DateTimeOffset.UtcNow,
                    "complete"));

            IPlanMaterializer materializer = provider.GetRequiredService<IPlanMaterializer>();
            IReadOnlyList<StoredPlan> stored = await materializer.MaterializeAsync(session, CancellationToken.None);

            Assert.Single(stored);
            Assert.Equal("auth-refactor", stored[0].PlanId);
            Assert.Equal("Auth refactor", stored[0].Title);

            string planFile = Path.Combine(workspacePath, ".orchi", "plan-auth-refactor.md");
            Assert.True(File.Exists(planFile));
            Assert.Contains("Add JWT auth.", await File.ReadAllTextAsync(planFile));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }

            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task MaterializeAsync_SyncsFromFilesWhenLatestMessageHasNoPlanBlocks()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"orchi-plan-mat-{Guid.NewGuid():N}.db");
        string workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-plan-ws-{Guid.NewGuid():N}");
        Guid sourceChatId = Guid.NewGuid();
        Directory.CreateDirectory(Path.Combine(workspacePath, ".orchi"));
        await File.WriteAllTextAsync(
            Path.Combine(workspacePath, ".orchi", "plan-ui-polish.md"),
            "# UI polish\n\nTighten spacing.\n");

        try
        {
            await using ServiceProvider provider = BuildProvider(databasePath);
            IDbContextFactory<AppDbContext> factory = provider.GetRequiredService<IDbContextFactory<AppDbContext>>();

            await using (AppDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.MigrateAsync();
                db.Chats.Add(new Chat
                {
                    Id = sourceChatId,
                    AgentId = "cursor",
                    WorkspacePath = workspacePath,
                    Mode = "orchestration",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
            }

            var session = new ChatSession
            {
                Id = sourceChatId,
                AgentId = "cursor",
                WorkspacePath = workspacePath,
                Mode = "orchestration"
            };
            session.Messages.Add(
                new ChatMessage(
                    Guid.NewGuid(),
                    "assistant",
                    "Updated the UI polish plan file in place.",
                    DateTimeOffset.UtcNow,
                    "complete"));

            IPlanMaterializer materializer = provider.GetRequiredService<IPlanMaterializer>();
            IReadOnlyList<StoredPlan> stored = await materializer.MaterializeAsync(session, CancellationToken.None);

            Assert.Single(stored);
            Assert.Equal("ui-polish", stored[0].PlanId);
            Assert.Contains("Tighten spacing.", stored[0].ContentMarkdown);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }

            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }

    private static ServiceProvider BuildProvider(string databasePath)
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));
        services.AddSingleton<EfPlanStore>();
        services.AddSingleton<IPlanStore>(sp => sp.GetRequiredService<EfPlanStore>());
        services.AddSingleton<OrchiArtifactFileStore>();
        services.AddSingleton<IOrchiArtifactWriterStrategy, ImplementationPlanWriterStrategy>();
        services.AddSingleton<IOrchiArtifactWriterStrategy, ReviewBriefWriterStrategy>();
        services.AddSingleton<IOrchiArtifactWriterFactory, OrchiArtifactWriterFactory>();
        services.AddSingleton<IPlanMaterializer, PlanMaterializer>();
        services.AddLogging();
        return services.BuildServiceProvider();
    }
}
