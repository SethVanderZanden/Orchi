using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;

namespace Orchi.Api.Infrastructure.Database;

public static class DatabaseExtensions
{
    public static IServiceCollection AddOrchiDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=orchi.db";

        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped(static sp =>
            sp.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

        return services;
    }

    public static async Task ApplyOrchiMigrationsAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        IDbContextFactory<AppDbContext> factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using AppDbContext db = await factory.CreateDbContextAsync();
        await db.Database.MigrateAsync();
    }
}
