using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Migrations;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class ProjectWorkspaceChatMigrationTests : IAsyncLifetime
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"orchi-migration-{Guid.NewGuid():N}.db");
    private SharedDatabaseWebApplicationFactory? _factory;
    private HttpClient? _client;
    private bool _disposed;

    public Task InitializeAsync()
    {
        _factory = new SharedDatabaseWebApplicationFactory(_databasePath);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        _disposed = true;
        _client?.Dispose();
        _factory?.Dispose();

        SqliteConnection.ClearAllPools();
        foreach (string path in new[] { _databasePath, $"{_databasePath}-wal", $"{_databasePath}-shm" })
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
            }
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task Migration_BackfillsExistingChatsFromWorkspacePath()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"orchi-migration-{Guid.NewGuid():N}.db");
        string workspaceA = Path.Combine(Path.GetTempPath(), $"orchi-migrate-a-{Guid.NewGuid():N}");
        string workspaceB = Path.Combine(Path.GetTempPath(), $"orchi-migrate-b-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspaceA);
        Directory.CreateDirectory(workspaceB);

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            await using (AppDbContext db = new(options))
            {
                await db.Database.MigrateAsync("20260703233135_AddPlansTable");

                DateTimeOffset now = DateTimeOffset.UtcNow;
                var chatA1 = Guid.NewGuid();
                var chatA2 = Guid.NewGuid();
                var chatB = Guid.NewGuid();

                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO Chats (Id, AgentId, WorkspacePath, Mode, CreatedAt, UpdatedAt, IsDeleted)
                    VALUES ({chatA1}, 'cursor', {workspaceA}, 'default', {now}, {now}, 0)
                    """);

                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO Chats (Id, AgentId, WorkspacePath, Mode, CreatedAt, UpdatedAt, IsDeleted)
                    VALUES ({chatB}, 'cursor', {workspaceB}, 'default', {now}, {now}, 0)
                    """);

                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO Chats (Id, AgentId, WorkspacePath, Mode, CreatedAt, UpdatedAt, IsDeleted)
                    VALUES ({chatA2}, 'cursor', {workspaceA}, 'default', {now}, {now}, 0)
                    """);
            }

            await using (AppDbContext db = new(options))
            {
                await db.Database.MigrateAsync();
                await ProjectWorkspaceMigrationBackfill.ApplyToContextAsync(db);
            }

            await using (AppDbContext db = new(options))
            {
                List<Chat> chats = await db.Chats.IgnoreQueryFilters().ToListAsync();
                Assert.Equal(3, chats.Count);
                Assert.All(chats, chat => Assert.NotNull(chat.ProjectId));
                Assert.All(chats, chat => Assert.NotNull(chat.WorkspaceId));

                Chat[] chatsForA = chats.Where(chat =>
                    string.Equals(Path.GetFullPath(chat.WorkspacePath), Path.GetFullPath(workspaceA), StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                Assert.Equal(2, chatsForA.Length);
                Assert.Equal(chatsForA[0].ProjectId, chatsForA[1].ProjectId);
                Assert.Equal(chatsForA[0].WorkspaceId, chatsForA[1].WorkspaceId);

                int projectCount = await db.Projects.CountAsync();
                int workspaceCount = await db.Workspaces.CountAsync();
                Assert.Equal(2, projectCount);
                Assert.Equal(2, workspaceCount);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            foreach (string path in new[] { databasePath, $"{databasePath}-wal", $"{databasePath}-shm" })
            {
                if (File.Exists(path))
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (IOException)
                    {
                    }
                }
            }

            if (Directory.Exists(workspaceA))
            {
                Directory.Delete(workspaceA, recursive: true);
            }

            if (Directory.Exists(workspaceB))
            {
                Directory.Delete(workspaceB, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DeleteProject_LeavesChatsWithNullProjectAndWorkspaceIds()
    {
        _factory!.InitializeDatabase();
        string workspace = Directory.GetCurrentDirectory();
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client!, workspace);

        HttpResponseMessage createResponse = await _client!.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor"));

        CreateChatResponse? created = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(created);
        Assert.NotNull(created.ProjectId);

        HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/projects/{created.ProjectId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        HttpResponseMessage getChat = await _client.GetAsync($"/chats/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getChat.StatusCode);

        ChatDetailResponse? detail =
            await getChat.Content.ReadFromJsonAsync<ChatDetailResponse>(HttpResponseExtensions.JsonOptions);
        Assert.NotNull(detail);
        Assert.Null(detail.ProjectId);
        Assert.Null(detail.WorkspaceId);
        Assert.Equal(created.WorkspacePath, detail.WorkspacePath);
    }
}
