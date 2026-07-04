using Microsoft.Extensions.Caching.Hybrid;

namespace Orchi.Api.Infrastructure.Caching;

public interface IOrchiCacheService
{
    ValueTask<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
}
