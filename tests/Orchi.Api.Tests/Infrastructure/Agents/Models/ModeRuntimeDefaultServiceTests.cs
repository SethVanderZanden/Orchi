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

public class ModeRuntimeDefaultServiceTests
{
    [Fact]
    public async Task ListAsync_ReturnsAllModesWithNullDefaultsWhenUnset()
    {
        await using ServiceProvider provider = BuildProvider();
        IModeRuntimeDefaultService service = provider.GetRequiredService<IModeRuntimeDefaultService>();

        IReadOnlyList<ModeRuntimeDefaultDto> defaults =
            await service.ListAsync(CancellationToken.None);

        Assert.Equal(4, defaults.Count);
        Assert.Equal(AgentModeIds.Default, defaults[0].Mode);
        Assert.Equal("Default", defaults[0].Label);
        Assert.Equal("cursor", defaults[0].AgentId);
        Assert.Null(defaults[0].ModelId);
        Assert.Null(defaults[0].ContextSizeId);
        Assert.Equal(AgentModeIds.Orchestration, defaults[1].Mode);
        Assert.Equal(AgentModeIds.Review, defaults[2].Mode);
        Assert.Equal(AgentModeIds.Implementation, defaults[3].Mode);
        Assert.All(defaults, dto => Assert.Null(dto.ModelId));
        Assert.All(defaults, dto => Assert.Null(dto.ContextSizeId));
    }

    [Fact]
    public async Task UpdateAsync_WithEnabledModel_PersistsDefault()
    {
        await using ServiceProvider provider = BuildProvider();
        IModeRuntimeDefaultService service = provider.GetRequiredService<IModeRuntimeDefaultService>();
        IAgentModelCatalogService catalog = provider.GetRequiredService<IAgentModelCatalogService>();

        await catalog.AddManualAsync("cursor", "gpt-5.3-codex", CancellationToken.None);

        Result<ModeRuntimeDefaultDto> updated = await service.UpdateAsync(
            AgentModeIds.Implementation,
            "cursor",
            "gpt-5.3-codex",
            null,
            null,
            null,
            CancellationToken.None);

        Assert.True(updated.IsSuccess);
        Assert.Equal("gpt-5.3-codex", updated.Value.ModelId);

        ModeRuntimeResolution resolved = await service.ResolveAsync(
            AgentModeIds.Implementation,
            CancellationToken.None);

        Assert.Equal("cursor", resolved.AgentId);
        Assert.Equal("gpt-5.3-codex", resolved.ModelId);
        Assert.Null(resolved.ContextSizeId);
    }

    [Fact]
    public async Task UpdateAsync_WithUnknownModel_ReturnsValidationError()
    {
        await using ServiceProvider provider = BuildProvider();
        IModeRuntimeDefaultService service = provider.GetRequiredService<IModeRuntimeDefaultService>();

        Result<ModeRuntimeDefaultDto> updated = await service.UpdateAsync(
            AgentModeIds.Default,
            "cursor",
            "nonexistent-model",
            null,
            null,
            null,
            CancellationToken.None);

        Assert.True(updated.IsFailure);
        Assert.Equal("Model.Unsupported", updated.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_WithUnsupportedMode_ReturnsValidationError()
    {
        await using ServiceProvider provider = BuildProvider();
        IModeRuntimeDefaultService service = provider.GetRequiredService<IModeRuntimeDefaultService>();

        Result<ModeRuntimeDefaultDto> updated = await service.UpdateAsync(
            "invalid-mode",
            "cursor",
            null,
            null,
            null,
            null,
            CancellationToken.None);

        Assert.True(updated.IsFailure);
        Assert.Equal("Mode.Unsupported", updated.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_ClearToCliDefault_SetsNull()
    {
        await using ServiceProvider provider = BuildProvider();
        IModeRuntimeDefaultService service = provider.GetRequiredService<IModeRuntimeDefaultService>();
        IAgentModelCatalogService catalog = provider.GetRequiredService<IAgentModelCatalogService>();

        await catalog.AddManualAsync("codex", "composer-2.5-fast", CancellationToken.None);
        await service.UpdateAsync(
            AgentModeIds.Review,
            "codex",
            "composer-2.5-fast",
            null,
            null,
            null,
            CancellationToken.None);

        Result<ModeRuntimeDefaultDto> cleared = await service.UpdateAsync(
            AgentModeIds.Review,
            "codex",
            null,
            null,
            null,
            null,
            CancellationToken.None);

        Assert.True(cleared.IsSuccess);
        Assert.Null(cleared.Value.ModelId);

        ModeRuntimeResolution resolved = await service.ResolveAsync(
            AgentModeIds.Review,
            CancellationToken.None);

        Assert.Equal("codex", resolved.AgentId);
        Assert.Null(resolved.ModelId);
    }

    [Fact]
    public void PickDefaultAgentId_PrefersBuiltInWhenEnabled()
    {
        Assert.Equal(
            "cursor",
            ModeRuntimeDefaultService.PickDefaultAgentId(AgentModeIds.Default, ["codex", "cursor"]));
        Assert.Equal(
            "codex",
            ModeRuntimeDefaultService.PickDefaultAgentId(AgentModeIds.Orchestration, ["codex", "cursor"]));
    }

    [Fact]
    public void PickDefaultAgentId_FallsBackToOnlyEnabledAgent()
    {
        Assert.Equal(
            "codex",
            ModeRuntimeDefaultService.PickDefaultAgentId(AgentModeIds.Default, ["codex"]));
        Assert.Equal(
            "cursor",
            ModeRuntimeDefaultService.PickDefaultAgentId(AgentModeIds.Review, ["cursor"]));
    }

    [Fact]
    public async Task ApplyEnabledAgentsAsync_SeedAllModes_WritesPreferredAgentsWithNullModels()
    {
        await using ServiceProvider provider = BuildProvider();
        IModeRuntimeDefaultService service = provider.GetRequiredService<IModeRuntimeDefaultService>();

        await service.ApplyEnabledAgentsAsync(["codex"], seedAllModes: true, CancellationToken.None);

        IReadOnlyList<ModeRuntimeDefaultDto> defaults =
            await service.ListAsync(CancellationToken.None);

        Assert.All(defaults, dto => Assert.Equal("codex", dto.AgentId));
        Assert.All(defaults, dto => Assert.Null(dto.ModelId));
        Assert.All(defaults, dto => Assert.Null(dto.ContextSizeId));
    }

    [Fact]
    public async Task ApplyEnabledAgentsAsync_WithoutSeed_OnlyRemapsDisabledAgents()
    {
        await using ServiceProvider provider = BuildProvider();
        IModeRuntimeDefaultService service = provider.GetRequiredService<IModeRuntimeDefaultService>();
        IAgentModelCatalogService catalog = provider.GetRequiredService<IAgentModelCatalogService>();

        await catalog.AddManualAsync("cursor", "claude-4.6-sonnet-medium-thinking", CancellationToken.None);
        await service.UpdateAsync(
            AgentModeIds.Default,
            "cursor",
            "claude-4.6-sonnet-medium-thinking",
            null,
            null,
            null,
            CancellationToken.None);
        await service.UpdateAsync(
            AgentModeIds.Orchestration,
            "codex",
            null,
            null,
            null,
            null,
            CancellationToken.None);

        await service.ApplyEnabledAgentsAsync(["cursor"], seedAllModes: false, CancellationToken.None);

        IReadOnlyList<ModeRuntimeDefaultDto> defaults =
            await service.ListAsync(CancellationToken.None);

        ModeRuntimeDefaultDto defaultMode = defaults.Single(dto => dto.Mode == AgentModeIds.Default);
        ModeRuntimeDefaultDto orchestration = defaults.Single(dto => dto.Mode == AgentModeIds.Orchestration);

        Assert.Equal("cursor", defaultMode.AgentId);
        Assert.Equal("claude-4.6-sonnet-medium-thinking", defaultMode.ModelId);
        Assert.Equal("cursor", orchestration.AgentId);
        Assert.Null(orchestration.ModelId);
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
        services.AddSingleton<IAgentAdapter, FakeCodexAdapter>();
        services.AddSingleton<IAgentAdapterFactory, AgentAdapterFactory>();
        services.AddSingleton<IAgentModeStrategy, DefaultAgentModeStrategy>();
        services.AddSingleton<IAgentModeStrategy, OrchestrationAgentModeStrategy>();
        services.AddSingleton<IAgentModeStrategy, ReviewAgentModeStrategy>();
        services.AddSingleton<IAgentModeStrategy, ImplementationAgentModeStrategy>();
        services.AddSingleton<FakeAgentModelListProvider>();
        services.AddSingleton<IAgentModelListProvider>(sp => sp.GetRequiredService<FakeAgentModelListProvider>());
        services.AddSingleton<AgentModelListProviderFactory>();
        services.AddSingleton<IAgentModelStore, EfAgentModelStore>();
        services.AddSingleton<IAgentContextSizeStore, EfAgentContextSizeStore>();
        services.AddSingleton<IAgentCliOptionStore, EfAgentCliOptionStore>();
        services.AddSingleton<IModeRuntimeDefaultStore, EfModeRuntimeDefaultStore>();
        services.AddSingleton<IAgentModelCatalogService, AgentModelCatalogService>();
        services.AddSingleton<IAgentContextSizeCatalogService, AgentContextSizeCatalogService>();
        services.AddSingleton<IAgentCliOptionCatalogService, AlwaysEnabledCliOptionCatalogService>();
        services.AddSingleton<IModeRuntimeDefaultService, ModeRuntimeDefaultService>();

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

    private sealed class FakeCodexAdapter : IAgentAdapter
    {
        public string AgentId => "codex";

        public IAsyncEnumerable<AgentEvent> SendMessageAsync(
            ChatSession session,
            string prompt,
            IReadOnlyList<string> extraCliArgs,
            CancellationToken cancellationToken) =>
            AsyncEnumerable.Empty<AgentEvent>();
    }

    private sealed class AlwaysEnabledCliOptionCatalogService : IAgentCliOptionCatalogService
    {
        public Task<IReadOnlyList<AgentCliOptionDto>> ListAsync(
            string agentId,
            string kind,
            bool includeDisabled,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AgentCliOptionDto>>([]);

        public Task<Result<AgentCliOptionDto>> AddManualAsync(
            string agentId,
            string kind,
            string optionId,
            string label,
            string? cliValue,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<Result<AgentCliOptionDto>> UpdateEnabledAsync(
            string agentId,
            string kind,
            string optionId,
            bool isEnabled,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<Result> RemoveAsync(
            string agentId,
            string kind,
            string optionId,
            CancellationToken cancellationToken) =>
            throw new NotImplementedException();

        public Task<bool> IsEnabledOptionAsync(
            string agentId,
            string kind,
            string optionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<string?> ResolveCliValueAsync(
            string agentId,
            string kind,
            string optionId,
            CancellationToken cancellationToken) =>
            Task.FromResult<string?>(optionId);
    }

    private sealed class FakeAgentModelListProvider : IAgentModelListProvider
    {
        public string AgentId => "cursor";

        public Task<IReadOnlyList<AgentModelListEntry>> FetchModelsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AgentModelListEntry>>([]);
    }
}
