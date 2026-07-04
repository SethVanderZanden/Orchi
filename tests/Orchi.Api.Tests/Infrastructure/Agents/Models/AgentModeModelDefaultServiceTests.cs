using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orchi.Api.Common.Results;
using Orchi.Api.Data;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Models;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.Api.Infrastructure.Caching;

namespace Orchi.Api.Tests.Infrastructure.Agents.Models;

public class AgentModeModelDefaultServiceTests
{
    [Fact]
    public async Task ListAsync_ReturnsAllModesWithNullDefaultsWhenUnset()
    {
        await using ServiceProvider provider = BuildProvider();
        IAgentModeModelDefaultService service = provider.GetRequiredService<IAgentModeModelDefaultService>();

        IReadOnlyList<AgentModeModelDefaultDto> defaults =
            await service.ListAsync("cursor", CancellationToken.None);

        Assert.Equal(4, defaults.Count);
        Assert.Equal(AgentModeIds.Default, defaults[0].Mode);
        Assert.Equal("Default", defaults[0].Label);
        Assert.Null(defaults[0].ModelId);
        Assert.Equal(AgentModeIds.Orchestration, defaults[1].Mode);
        Assert.Equal(AgentModeIds.Review, defaults[2].Mode);
        Assert.Equal(AgentModeIds.Implementation, defaults[3].Mode);
        Assert.All(defaults, dto => Assert.Null(dto.ModelId));
    }

    [Fact]
    public async Task UpdateAsync_WithEnabledModel_PersistsDefault()
    {
        await using ServiceProvider provider = BuildProvider();
        IAgentModeModelDefaultService service = provider.GetRequiredService<IAgentModeModelDefaultService>();
        IAgentModelCatalogService catalog = provider.GetRequiredService<IAgentModelCatalogService>();

        await catalog.AddManualAsync("cursor", "gpt-5.3-codex", CancellationToken.None);

        Result<AgentModeModelDefaultDto> updated = await service.UpdateAsync(
            "cursor",
            AgentModeIds.Implementation,
            "gpt-5.3-codex",
            CancellationToken.None);

        Assert.True(updated.IsSuccess);
        Assert.Equal("gpt-5.3-codex", updated.Value.ModelId);

        string? resolved = await service.ResolveAsync(
            "cursor",
            AgentModeIds.Implementation,
            CancellationToken.None);

        Assert.Equal("gpt-5.3-codex", resolved);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownModel_ReturnsValidationError()
    {
        await using ServiceProvider provider = BuildProvider();
        IAgentModeModelDefaultService service = provider.GetRequiredService<IAgentModeModelDefaultService>();

        Result<AgentModeModelDefaultDto> updated = await service.UpdateAsync(
            "cursor",
            AgentModeIds.Default,
            "nonexistent-model",
            CancellationToken.None);

        Assert.True(updated.IsFailure);
        Assert.Equal("Model.Unsupported", updated.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_WithUnsupportedMode_ReturnsValidationError()
    {
        await using ServiceProvider provider = BuildProvider();
        IAgentModeModelDefaultService service = provider.GetRequiredService<IAgentModeModelDefaultService>();

        Result<AgentModeModelDefaultDto> updated = await service.UpdateAsync(
            "cursor",
            "invalid-mode",
            null,
            CancellationToken.None);

        Assert.True(updated.IsFailure);
        Assert.Equal("Mode.Unsupported", updated.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_ClearToCliDefault_SetsNull()
    {
        await using ServiceProvider provider = BuildProvider();
        IAgentModeModelDefaultService service = provider.GetRequiredService<IAgentModeModelDefaultService>();
        IAgentModelCatalogService catalog = provider.GetRequiredService<IAgentModelCatalogService>();

        await catalog.AddManualAsync("cursor", "composer-2.5-fast", CancellationToken.None);
        await service.UpdateAsync(
            "cursor",
            AgentModeIds.Review,
            "composer-2.5-fast",
            CancellationToken.None);

        Result<AgentModeModelDefaultDto> cleared = await service.UpdateAsync(
            "cursor",
            AgentModeIds.Review,
            null,
            CancellationToken.None);

        Assert.True(cleared.IsSuccess);
        Assert.Null(cleared.Value.ModelId);

        string? resolved = await service.ResolveAsync(
            "cursor",
            AgentModeIds.Review,
            CancellationToken.None);

        Assert.Null(resolved);
    }

    private static ServiceProvider BuildProvider()
    {
        string databasePath = Path.Combine(Path.GetTempPath(), $"orchi-mode-defaults-{Guid.NewGuid():N}.db");

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
        services.AddSingleton<IAgentModeStrategy, DefaultAgentModeStrategy>();
        services.AddSingleton<IAgentModeStrategy, OrchestrationAgentModeStrategy>();
        services.AddSingleton<IAgentModeStrategy, ReviewAgentModeStrategy>();
        services.AddSingleton<IAgentModeStrategy, ImplementationAgentModeStrategy>();
        services.AddSingleton<FakeAgentModelListProvider>();
        services.AddSingleton<IAgentModelListProvider>(sp => sp.GetRequiredService<FakeAgentModelListProvider>());
        services.AddSingleton<AgentModelListProviderFactory>();
        services.AddSingleton<IAgentModelStore, EfAgentModelStore>();
        services.AddSingleton<IAgentModeModelDefaultStore, EfAgentModeModelDefaultStore>();
        services.AddSingleton<IAgentModelCatalogService, AgentModelCatalogService>();
        services.AddSingleton<IAgentModeModelDefaultService, AgentModeModelDefaultService>();

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

        public Task<IReadOnlyList<AgentModelListEntry>> FetchModelsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AgentModelListEntry>>([]);
    }
}
