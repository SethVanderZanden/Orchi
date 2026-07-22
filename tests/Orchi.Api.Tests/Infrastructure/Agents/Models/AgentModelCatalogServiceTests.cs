using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orchi.Api.Common.Results;
using Orchi.Api.Data;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Models;
using Orchi.Api.Infrastructure.Caching;

namespace Orchi.Api.Tests.Infrastructure.Agents.Models;

public class AgentModelCatalogServiceTests
{
    [Fact]
    public async Task SyncAsync_MergesCliModelsAndDisablesMissingOnes()
    {
        await using ServiceProvider provider = BuildProvider();
        IAgentModelCatalogService catalog = provider.GetRequiredService<IAgentModelCatalogService>();
        FakeAgentModelListProvider listProvider = provider.GetRequiredService<FakeAgentModelListProvider>();

        listProvider.Models =
        [
            new AgentModelListEntry("model-a", "model-a", IsDefault: true, IsCurrent: false),
            new AgentModelListEntry("model-b", "model-b", IsDefault: false, IsCurrent: true)
        ];

        Result<AgentModelSyncResult> firstSync = await catalog.SyncAsync("cursor", CancellationToken.None);
        Assert.True(firstSync.IsSuccess);
        Assert.Equal(2, firstSync.Value.Models.Count);

        listProvider.Models =
        [
            new AgentModelListEntry("model-b", "model-b", IsDefault: false, IsCurrent: true),
            new AgentModelListEntry("model-c", "model-c", IsDefault: false, IsCurrent: false)
        ];

        Result<AgentModelSyncResult> secondSync = await catalog.SyncAsync("cursor", CancellationToken.None);
        Assert.True(secondSync.IsSuccess);

        IReadOnlyList<AgentModelDto> allModels = await catalog.ListAsync("cursor", includeDisabled: true, CancellationToken.None);
        AgentModelDto? modelA = allModels.FirstOrDefault(model => model.Id == "model-a");
        AgentModelDto? modelB = allModels.FirstOrDefault(model => model.Id == "model-b");
        AgentModelDto? modelC = allModels.FirstOrDefault(model => model.Id == "model-c");

        Assert.NotNull(modelA);
        Assert.False(modelA.IsEnabled);
        Assert.NotNull(modelB);
        Assert.True(modelB.IsEnabled);
        Assert.NotNull(modelC);
        Assert.True(modelC.IsEnabled);
    }

    [Fact]
    public async Task AddManualAsync_AllowsSelectionAfterSync()
    {
        await using ServiceProvider provider = BuildProvider();
        IAgentModelCatalogService catalog = provider.GetRequiredService<IAgentModelCatalogService>();

        Result<AgentModelDto> added = await catalog.AddManualAsync("cursor", "custom-model", label: null, CancellationToken.None);
        Assert.True(added.IsSuccess);

        bool enabled = await catalog.IsEnabledModelAsync("cursor", "custom-model", CancellationToken.None);
        Assert.True(enabled);
    }

    [Fact]
    public async Task RemoveAsync_RemovesManualAndCliEntries()
    {
        await using ServiceProvider provider = BuildProvider();
        IAgentModelCatalogService catalog = provider.GetRequiredService<IAgentModelCatalogService>();
        FakeAgentModelListProvider listProvider = provider.GetRequiredService<FakeAgentModelListProvider>();

        listProvider.Models = [new AgentModelListEntry("cli-model", "cli-model", false, false)];
        await catalog.SyncAsync("cursor", CancellationToken.None);
        await catalog.AddManualAsync("cursor", "manual-model", label: null, CancellationToken.None);

        Result removedCli = await catalog.RemoveAsync("cursor", "cli-model", CancellationToken.None);
        Assert.True(removedCli.IsSuccess);

        Result removedManual = await catalog.RemoveAsync("cursor", "manual-model", CancellationToken.None);
        Assert.True(removedManual.IsSuccess);

        IReadOnlyList<AgentModelDto> remaining =
            await catalog.ListAsync("cursor", includeDisabled: true, CancellationToken.None);

        Assert.Empty(remaining);
    }

    [Fact]
    public async Task RemoveAsync_WhenMissing_ReturnsNotFound()
    {
        await using ServiceProvider provider = BuildProvider();
        IAgentModelCatalogService catalog = provider.GetRequiredService<IAgentModelCatalogService>();

        Result removed = await catalog.RemoveAsync("cursor", "missing-model", CancellationToken.None);
        Assert.True(removed.IsFailure);
    }

    private static ServiceProvider BuildProvider()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"orchi-model-catalog-{Guid.NewGuid():N}.db");

        var services = new ServiceCollection();
        services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cache:DefaultExpirationMinutes"] = "5",
                ["Cache:AgentModelsExpirationMinutes"] = "1440",
                ["Cache:Distributed:Enabled"] = "false"
            })
            .Build();

        services.AddOrchiCaching(configuration);
        services.AddSingleton<IAgentAdapter, FakeCursorAdapter>();
        services.AddSingleton<IAgentAdapterFactory, AgentAdapterFactory>();
        services.AddSingleton<FakeAgentModelListProvider>();
        services.AddSingleton<IAgentModelListProvider>(sp => sp.GetRequiredService<FakeAgentModelListProvider>());
        services.AddSingleton<AgentModelListProviderFactory>();
        services.AddSingleton<Orchi.Api.Infrastructure.Agents.Persistence.IAgentModelStore,
            Orchi.Api.Infrastructure.Agents.Persistence.EfAgentModelStore>();
        services.AddSingleton<IAgentModelCatalogService, AgentModelCatalogService>();

        ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        IDbContextFactory<AppDbContext> factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using AppDbContext db = factory.CreateDbContext();
        db.Database.Migrate();

        return provider;
    }

    private sealed class FakeCursorAdapter : IAgentAdapter
    {
        public string AgentId => "cursor";

        public IAsyncEnumerable<AgentEvent> SendMessageAsync(
            ChatSession session,
            string prompt,
            IReadOnlyList<string> extraCliArgs,
            CancellationToken cancellationToken) =>
            AsyncEnumerable.Empty<AgentEvent>();
    }

    private sealed class FakeAgentModelListProvider : IAgentModelListProvider
    {
        public string AgentId => "cursor";

        public IReadOnlyList<AgentModelListEntry> Models { get; set; } = [];

        public Task<IReadOnlyList<AgentModelListEntry>> FetchModelsAsync(CancellationToken cancellationToken) =>
            Task.FromResult(Models);
    }
}
