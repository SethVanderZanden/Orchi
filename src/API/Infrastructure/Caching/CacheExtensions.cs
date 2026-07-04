using Microsoft.Extensions.Caching.Hybrid;

namespace Orchi.Api.Infrastructure.Caching;

public static class CacheExtensions
{
    public static IServiceCollection AddOrchiCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        OrchiCacheOptions cacheOptions = configuration
            .GetSection(OrchiCacheOptions.SectionName)
            .Get<OrchiCacheOptions>() ?? new OrchiCacheOptions();

        // When Distributed:Enabled is true and Provider is Redis, register
        // AddStackExchangeRedisCache before AddHybridCache so HybridCache uses L2.
        // Not implemented yet — local memory-only L1 for now.
        if (cacheOptions.Distributed.Enabled &&
            string.Equals(cacheOptions.Distributed.Provider, "Redis", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Distributed cache is not configured yet. Set Cache:Distributed:Enabled to false " +
                "or register AddStackExchangeRedisCache before enabling Redis.");
        }

        services.Configure<OrchiCacheOptions>(configuration.GetSection(OrchiCacheOptions.SectionName));

        services.AddHybridCache(options =>
        {
            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(cacheOptions.DefaultExpirationMinutes),
                LocalCacheExpiration = TimeSpan.FromMinutes(cacheOptions.DefaultExpirationMinutes)
            };
        });

        services.AddSingleton<OrchiHybridCacheService>();
        services.AddSingleton<IOrchiCacheService>(sp => sp.GetRequiredService<OrchiHybridCacheService>());

        return services;
    }
}
