using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Orchi.Api.Common;
using Orchi.Api.Data;
using Orchi.Api.Entities;

namespace Orchi.Api.Migrations;

internal static class ProjectWorkspaceMigrationBackfill
{
    internal static void Apply(MigrationBuilder migrationBuilder)
    {
        DbConnection? connection = TryGetConnection(migrationBuilder);
        if (connection is null)
        {
            return;
        }

        RunBackfill(connection);
    }

    internal static async Task ApplyToContextAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        bool needsBackfill = await db.Chats
            .IgnoreQueryFilters()
            .AnyAsync(
                chat => (chat.ProjectId == null || chat.WorkspaceId == null)
                        && !string.IsNullOrWhiteSpace(chat.WorkspacePath),
                cancellationToken);

        if (!needsBackfill)
        {
            return;
        }

        List<Chat> chats = await db.Chats
            .IgnoreQueryFilters()
            .Where(chat => (chat.ProjectId == null || chat.WorkspaceId == null)
                           && !string.IsNullOrWhiteSpace(chat.WorkspacePath))
            .ToListAsync(cancellationToken);

        var pathToWorkspace = new Dictionary<string, Workspace>(StringComparer.Ordinal);

        List<Workspace> existingWorkspaces = await db.Workspaces.AsNoTracking().ToListAsync(cancellationToken);
        foreach (Workspace workspace in existingWorkspaces)
        {
            pathToWorkspace.TryAdd(workspace.NormalizedPath, workspace);
        }

        foreach (string normalizedPath in chats
                     .Select(chat => WorkspacePathNormalizer.Normalize(chat.WorkspacePath))
                     .Distinct(StringComparer.Ordinal))
        {
            if (pathToWorkspace.ContainsKey(normalizedPath))
            {
                continue;
            }

            string samplePath = chats.First(chat =>
                WorkspacePathNormalizer.Normalize(chat.WorkspacePath) == normalizedPath).WorkspacePath;

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(samplePath);
            }
            catch
            {
                fullPath = samplePath;
            }

            string name = WorkspacePathNormalizer.DeriveNameFromPath(fullPath);
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = name,
                CreatedAt = now,
                UpdatedAt = now
            };

            var workspace = new Workspace
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Path = fullPath,
                NormalizedPath = normalizedPath,
                Name = name,
                IsDefault = true,
                Kind = WorkspaceKind.Primary,
                CreatedAt = now
            };

            db.Projects.Add(project);
            db.Workspaces.Add(workspace);
            pathToWorkspace[normalizedPath] = workspace;
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (Chat chat in chats)
        {
            string normalizedPath = WorkspacePathNormalizer.Normalize(chat.WorkspacePath);
            if (!pathToWorkspace.TryGetValue(normalizedPath, out Workspace? workspace))
            {
                continue;
            }

            chat.ProjectId = workspace.ProjectId;
            chat.WorkspaceId = workspace.Id;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static DbConnection? TryGetConnection(MigrationBuilder migrationBuilder)
    {
        if (migrationBuilder is not IInfrastructure<IServiceProvider> infrastructure)
        {
            return null;
        }

        IRelationalConnection? relationalConnection =
            infrastructure.Instance.GetService(typeof(IRelationalConnection)) as IRelationalConnection;

        return relationalConnection?.DbConnection;
    }

    private static void RunBackfill(DbConnection connection)
    {
        bool wasClosed = connection.State == System.Data.ConnectionState.Closed;
        if (wasClosed)
        {
            connection.Open();
        }

        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            if (connection is Microsoft.Data.Sqlite.SqliteConnection sqliteConnection)
            {
                optionsBuilder.UseSqlite(sqliteConnection);
            }
            else
            {
                return;
            }

            using AppDbContext db = new(optionsBuilder.Options);
            ApplyToContextAsync(db).GetAwaiter().GetResult();
        }
        finally
        {
            if (wasClosed)
            {
                connection.Close();
            }
        }
    }
}
