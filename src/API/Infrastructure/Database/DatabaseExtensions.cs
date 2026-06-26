using Microsoft.EntityFrameworkCore;
using Orchi.Api.Data;

namespace Orchi.Api.Infrastructure.Database;

public static class DatabaseExtensions
{
    public static IServiceCollection AddOrchiDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}
