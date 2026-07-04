using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orchi.Api.Infrastructure.Caching;

namespace Orchi.Api.Tests.Infrastructure.Caching;

public class OrchiHybridCacheServiceTests
{
    [Fact]
    public async Task GetOrCreateAsync_ExecutesFactoryOnceForSameKey()
    {
        await using ServiceProvider provider = CreateProvider();
        IOrchiCacheService cache = provider.GetRequiredService<IOrchiCacheService>();

        int factoryCalls = 0;

        string first = await cache.GetOrCreateAsync(
            "test-key",
            _ =>
            {
                factoryCalls++;
                return ValueTask.FromResult("value");
            }).AsTask();

        string second = await cache.GetOrCreateAsync(
            "test-key",
            _ =>
            {
                factoryCalls++;
                return ValueTask.FromResult("other");
            }).AsTask();

        Assert.Equal("value", first);
        Assert.Equal("value", second);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task RemoveAsync_ClearsEntrySoFactoryRunsAgain()
    {
        await using ServiceProvider provider = CreateProvider();
        IOrchiCacheService cache = provider.GetRequiredService<IOrchiCacheService>();

        int factoryCalls = 0;

        await cache.GetOrCreateAsync(
            "remove-key",
            _ =>
            {
                factoryCalls++;
                return ValueTask.FromResult(1);
            }).AsTask();

        await cache.RemoveAsync("remove-key");

        int second = await cache.GetOrCreateAsync(
            "remove-key",
            _ =>
            {
                factoryCalls++;
                return ValueTask.FromResult(2);
            }).AsTask();

        Assert.Equal(2, second);
        Assert.Equal(2, factoryCalls);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:DefaultExpirationMinutes"] = "5",
                ["Cache:WorkspaceDiffExpirationSeconds"] = "30",
                ["Cache:CursorExecutableExpirationMinutes"] = "60",
                ["Cache:PlanExpirationMinutes"] = "10",
                ["Cache:Distributed:Enabled"] = "false"
            })
            .Build();

        services.AddOrchiCaching(configuration);
        return services.BuildServiceProvider();
    }
}
