using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Infrastructure.Agents.Persistence;

namespace Orchi.Api.Tests.Common;

public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"orchi-test-{Guid.NewGuid():N}.db");
    private bool _databaseInitialized;

    public string DatabasePath => _databasePath;

    public void InitializeDatabase() => EnsureDatabase();

    public Task ClearAllChatsAsync(CancellationToken cancellationToken = default) =>
        TestDatabaseCleaner.ClearAllChatsAsync(Services, cancellationToken);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextFactory<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IChatStore>();

            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite($"Data Source={_databasePath}"));

            services.AddScoped(static sp =>
                sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

            services.AddSingleton<IChatStore, EfChatStore>();
        });
    }

    private void EnsureDatabase()
    {
        if (_databaseInitialized)
        {
            return;
        }

        using IServiceScope scope = Services.CreateScope();
        IDbContextFactory<AppDbContext> factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using AppDbContext db = factory.CreateDbContext();
        db.Database.Migrate();
        _databaseInitialized = true;
    }

    public HttpClient CreateClientWithServices(Action<IServiceCollection>? configureServices = null)
    {
        if (configureServices is null)
        {
            InitializeDatabase();
            return CreateClient();
        }

        return WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(configureServices);
        }).CreateClient();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        TryDeleteDatabase();
    }

    private void TryDeleteDatabase()
    {
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
    }
}

public sealed class SharedDatabaseWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath;
    private bool _databaseInitialized;

    public SharedDatabaseWebApplicationFactory(string databasePath)
    {
        _databasePath = databasePath;
    }

    public string DatabasePath => _databasePath;

    public void InitializeDatabase() => EnsureDatabase();

    public Task ClearAllChatsAsync(CancellationToken cancellationToken = default) =>
        TestDatabaseCleaner.ClearAllChatsAsync(Services, cancellationToken);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbContextFactory<AppDbContext>>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<IChatStore>();

            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite($"Data Source={_databasePath}"));

            services.AddScoped(static sp =>
                sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

            services.AddSingleton<IChatStore, EfChatStore>();
        });
    }

    private void EnsureDatabase()
    {
        if (_databaseInitialized)
        {
            return;
        }

        using IServiceScope scope = Services.CreateScope();
        IDbContextFactory<AppDbContext> factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using AppDbContext db = factory.CreateDbContext();
        db.Database.Migrate();
        _databaseInitialized = true;
    }
}

internal static class TestDatabaseCleaner
{
    public static async Task ClearAllChatsAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using IServiceScope scope = services.CreateScope();
        IDbContextFactory<AppDbContext> factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using AppDbContext db = await factory.CreateDbContextAsync(cancellationToken);
        db.Plans.RemoveRange(await db.Plans.ToListAsync(cancellationToken));
        List<Chat> chats = await db.Chats.IgnoreQueryFilters().ToListAsync(cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (Chat chat in chats)
        {
            chat.IsDeleted = true;
            chat.UpdatedAt = now;
        }

        db.Workspaces.RemoveRange(await db.Workspaces.ToListAsync(cancellationToken));
        db.Projects.RemoveRange(await db.Projects.ToListAsync(cancellationToken));

        await db.SaveChangesAsync(cancellationToken);
    }
}

public static class HttpResponseExtensions
{
    public static async Task<T?> ReadJsonAsync<T>(this HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<T>();
    }

    public static async Task<HttpStatusCode> GetStatusCodeAsync(this Task<HttpResponseMessage> responseTask)
    {
        using HttpResponseMessage response = await responseTask;
        return response.StatusCode;
    }
}
