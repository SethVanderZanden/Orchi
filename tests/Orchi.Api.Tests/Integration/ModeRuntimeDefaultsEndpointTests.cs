using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orchi.Api.Data;
using Orchi.Api.Features.Agents.AddAgentModel;
using Orchi.Api.Features.Agents.ListModeRuntimeDefaults;
using Orchi.Api.Features.Agents.UpdateModeRuntimeDefault;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

[CollectionDefinition("ModeRuntimeDefaultsTests", DisableParallelization = true)]
public sealed class ModeRuntimeDefaultsTestsCollection;

[Collection("ModeRuntimeDefaultsTests")]
public class ModeRuntimeDefaultsEndpointTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ModeRuntimeDefaultsEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        factory.InitializeDatabase();
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await ClearModeDefaultsAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task ClearModeDefaultsAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        IDbContextFactory<AppDbContext> factory =
            scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using AppDbContext db = await factory.CreateDbContextAsync();
        db.ModeRuntimeDefaults.RemoveRange(await db.ModeRuntimeDefaults.ToListAsync());
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ListModeRuntimeDefaults_ReturnsFourModesWithNullDefaultsInitially()
    {
        await ClearModeDefaultsAsync();

        HttpResponseMessage response = await _client.GetAsync("/agents/mode-defaults");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ListModeRuntimeDefaults.Response? body =
            await response.Content.ReadFromJsonAsync<ListModeRuntimeDefaults.Response>();

        Assert.NotNull(body);
        Assert.Equal(4, body.Defaults.Count);
        Assert.Equal(AgentModeIds.Default, body.Defaults[0].Mode);
        Assert.Equal("Default", body.Defaults[0].Label);
        Assert.Equal("cursor", body.Defaults[0].AgentId);
        Assert.Null(body.Defaults[0].ModelId);
        Assert.Null(body.Defaults[0].ContextSizeId);
        Assert.Equal(AgentModeIds.Orchestration, body.Defaults[1].Mode);
        Assert.Equal(AgentModeIds.Review, body.Defaults[2].Mode);
        Assert.Equal(AgentModeIds.Implementation, body.Defaults[3].Mode);
        Assert.All(body.Defaults, row => Assert.Null(row.ModelId));
        Assert.All(body.Defaults, row => Assert.Null(row.ContextSizeId));
    }

    [Fact]
    public async Task UpdateModeRuntimeDefault_WithEnabledModel_Persists()
    {
        await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request("claude-4.6-sonnet-medium-thinking"));

        HttpResponseMessage patchResponse = await _client.PatchAsJsonAsync(
            $"/agents/mode-defaults/{AgentModeIds.Orchestration}",
            new UpdateModeRuntimeDefault.Request("cursor", "claude-4.6-sonnet-medium-thinking", null, null, null));

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        UpdateModeRuntimeDefault.DefaultResponse? patched =
            await patchResponse.Content.ReadFromJsonAsync<UpdateModeRuntimeDefault.DefaultResponse>();

        Assert.NotNull(patched);
        Assert.Equal(AgentModeIds.Orchestration, patched.Mode);
        Assert.Equal("cursor", patched.AgentId);
        Assert.Equal("claude-4.6-sonnet-medium-thinking", patched.ModelId);
        Assert.Null(patched.ContextSizeId);

        HttpResponseMessage listResponse = await _client.GetAsync("/agents/mode-defaults");
        ListModeRuntimeDefaults.Response? listBody =
            await listResponse.Content.ReadFromJsonAsync<ListModeRuntimeDefaults.Response>();

        Assert.NotNull(listBody);
        ListModeRuntimeDefaults.DefaultResponse? orchestration = listBody.Defaults
            .FirstOrDefault(row => row.Mode == AgentModeIds.Orchestration);

        Assert.NotNull(orchestration);
        Assert.Equal("claude-4.6-sonnet-medium-thinking", orchestration.ModelId);
    }

    [Fact]
    public async Task UpdateModeRuntimeDefault_WithUnknownModel_ReturnsValidationError()
    {
        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"/agents/mode-defaults/{AgentModeIds.Default}",
            new UpdateModeRuntimeDefault.Request("cursor", "nonexistent-model", null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateModeRuntimeDefault_WithDisabledModel_ReturnsValidationError()
    {
        await _client.PostAsJsonAsync(
            "/agents/codex/models",
            new AddAgentModel.Request("gpt-5.3-codex"));

        await _client.PatchAsJsonAsync(
            "/agents/codex/models",
            new Orchi.Api.Features.Agents.UpdateAgentModel.UpdateAgentModel.Request("gpt-5.3-codex", false));

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"/agents/mode-defaults/{AgentModeIds.Review}",
            new UpdateModeRuntimeDefault.Request("codex", "gpt-5.3-codex", null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateModeRuntimeDefault_WithUnsupportedAgent_ReturnsValidationError()
    {
        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"/agents/mode-defaults/{AgentModeIds.Default}",
            new UpdateModeRuntimeDefault.Request("unknown", null, null, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
