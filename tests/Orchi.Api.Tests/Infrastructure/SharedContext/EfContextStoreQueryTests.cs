using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orchi.SharedContext;
using Orchi.SharedContext.Storage;
using Orchi.SharedContext.Storage.Entities;

namespace Orchi.Api.Tests.Infrastructure.SharedContext;

public class EfContextStoreQueryTests
{
    [Fact]
    public async Task QueryAsync_OrdersByMostRecentlyIndexedFirst()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"orchi-context-unit-{Guid.NewGuid():N}.db");
        string workspacePath = Directory.GetCurrentDirectory();

        try
        {
            var services = new ServiceCollection();
            services.AddDbContextFactory<SharedContextDbContext>(options =>
                options.UseSqlite($"Data Source={databasePath}"));

            await using ServiceProvider provider = services.BuildServiceProvider();
            IDbContextFactory<SharedContextDbContext> factory =
                provider.GetRequiredService<IDbContextFactory<SharedContextDbContext>>();

            var workspaceId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;

            await using (SharedContextDbContext db = await factory.CreateDbContextAsync())
            {
                await db.Database.EnsureCreatedAsync();

                db.Workspaces.Add(new WorkspaceEntity
                {
                    Id = workspaceId,
                    NormalizedPath = WorkspacePathNormalizer.Normalize(workspacePath),
                    CreatedAt = now,
                    UpdatedAt = now
                });

                db.IndexedFiles.Add(new IndexedFileEntity
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = workspaceId,
                    RelativePath = "older-queryterm-file.cs",
                    ContentHash = "hash-old",
                    IndexedAt = now.AddHours(-2)
                });

                db.IndexedFiles.Add(new IndexedFileEntity
                {
                    Id = Guid.NewGuid(),
                    WorkspaceId = workspaceId,
                    RelativePath = "newer-queryterm-file.cs",
                    ContentHash = "hash-new",
                    IndexedAt = now
                });

                await db.SaveChangesAsync();
            }

            var store = new EfContextStore(factory, Options.Create(new SharedContextOptions()));
            IReadOnlyList<ContextChunk> results = await store.QueryAsync(
                new ContextQuery(workspacePath, "queryterm", TopK: 8),
                CancellationToken.None);

            Assert.Equal(2, results.Count);
            Assert.Equal("newer-queryterm-file.cs", results[0].SourcePath);
            Assert.Equal("older-queryterm-file.cs", results[1].SourcePath);
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
