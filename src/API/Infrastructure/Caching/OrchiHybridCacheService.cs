using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Orchi.Api.Infrastructure.Caching;

public sealed class OrchiHybridCacheService(
    HybridCache hybridCache,
    IOptions<OrchiCacheOptions> options,
    ILogger<OrchiHybridCacheService> logger) : IOrchiCacheService
{
    private const string KeyPrefix = "orchi:";

    private readonly OrchiCacheOptions _options = options.Value;

    public async ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        string prefixedKey = PrefixKey(key);

        return await hybridCache.GetOrCreateAsync(
            prefixedKey,
            async ct =>
            {
                logger.LogDebug("Cache miss for key {CacheKey}; executing factory", prefixedKey);
                return await factory(ct);
            },
            options ?? CreateDefaultEntryOptions(),
            cancellationToken: cancellationToken);
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        hybridCache.RemoveAsync(PrefixKey(key), cancellationToken);

    public HybridCacheEntryOptions CreateDefaultEntryOptions() =>
        new()
        {
            Expiration = TimeSpan.FromMinutes(_options.DefaultExpirationMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(_options.DefaultExpirationMinutes)
        };

    public HybridCacheEntryOptions CreateWorkspaceDiffEntryOptions() =>
        CreateEntryOptions(TimeSpan.FromSeconds(_options.WorkspaceDiffExpirationSeconds));

    public HybridCacheEntryOptions CreateCursorExecutableEntryOptions() =>
        CreateEntryOptions(TimeSpan.FromMinutes(_options.CursorExecutableExpirationMinutes));

    public HybridCacheEntryOptions CreatePlanEntryOptions() =>
        CreateEntryOptions(TimeSpan.FromMinutes(_options.PlanExpirationMinutes));

    private static HybridCacheEntryOptions CreateEntryOptions(TimeSpan expiration) =>
        new()
        {
            Expiration = expiration,
            LocalCacheExpiration = expiration
        };

    private static string PrefixKey(string key) => KeyPrefix + key;
}
