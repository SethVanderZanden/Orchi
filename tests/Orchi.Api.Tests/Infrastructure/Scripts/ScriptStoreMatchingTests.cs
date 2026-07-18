using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Scripts;

namespace Orchi.Api.Tests.Infrastructure.Scripts;

public class ScriptStoreMatchingTests
{
    [Fact]
    public async Task ListMatchingAsync_PrefersProjectOverGlobal_AndFiltersMode()
    {
        await using var dbFactory = new TestDbContextFactory();
        var store = new EfScriptStore(dbFactory);

        Guid projectId = Guid.NewGuid();
        await using (AppDbContext seed = await dbFactory.CreateDbContextAsync(CancellationToken.None))
        {
            seed.Projects.Add(new Project
            {
                Id = projectId,
                Name = "Demo",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            await seed.SaveChangesAsync();
        }

        string steps = ScriptStepsSerializer.Serialize(
        [
            new ScriptStepDto(ScriptStepKinds.Shell, Command: "echo hi")
        ]);

        await store.CreateAsync(
            "Global any",
            null,
            steps,
            [new ScriptUpsertBinding(ScriptEventKind.AgentFinish, null, 10, true, ScriptOnError.Continue)],
            CancellationToken.None);

        await store.CreateAsync(
            "Project implementation",
            projectId,
            steps,
            [
                new ScriptUpsertBinding(
                    ScriptEventKind.AgentFinish,
                    "implementation",
                    0,
                    true,
                    ScriptOnError.Continue)
            ],
            CancellationToken.None);

        await store.CreateAsync(
            "Project review only",
            projectId,
            steps,
            [
                new ScriptUpsertBinding(
                    ScriptEventKind.AgentFinish,
                    "review",
                    0,
                    true,
                    ScriptOnError.Continue)
            ],
            CancellationToken.None);

        IReadOnlyList<StoredScript> matching = await store.ListMatchingAsync(
            ScriptEventKind.AgentFinish,
            projectId,
            "implementation",
            CancellationToken.None);

        Assert.Equal(2, matching.Count);
        Assert.Equal("Project implementation", matching[0].Name);
        Assert.Equal("Global any", matching[1].Name);
    }

    private sealed class TestDbContextFactory : IDbContextFactory<AppDbContext>, IAsyncDisposable
    {
        private readonly string _dbPath = Path.Combine(
            Path.GetTempPath(),
            $"orchi-script-tests-{Guid.NewGuid():N}.db");

        public AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={_dbPath}")
                .Options;

            var db = new AppDbContext(options);
            db.Database.EnsureCreated();
            return db;
        }

        public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());

        public ValueTask DisposeAsync()
        {
            try
            {
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch (IOException)
            {
                // Best-effort cleanup; SQLite may still hold a handle briefly.
            }

            return ValueTask.CompletedTask;
        }
    }
}
