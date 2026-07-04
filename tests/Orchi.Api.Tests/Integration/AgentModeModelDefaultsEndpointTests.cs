using System.Net;
using System.Net.Http.Json;
using Orchi.Api.Features.Agents.AddAgentModel;
using Orchi.Api.Features.Agents.ListAgentModeModelDefaults;
using Orchi.Api.Features.Agents.UpdateAgentModeModelDefault;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class AgentModeModelDefaultsEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AgentModeModelDefaultsEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        factory.InitializeDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListAgentModeModelDefaults_ReturnsFourModesWithNullDefaultsInitially()
    {
        HttpResponseMessage response = await _client.GetAsync("/agents/cursor/mode-model-defaults");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ListAgentModeModelDefaults.Response? body =
            await response.Content.ReadFromJsonAsync<ListAgentModeModelDefaults.Response>();

        Assert.NotNull(body);
        Assert.Equal(4, body.Defaults.Count);
        Assert.Equal(AgentModeIds.Default, body.Defaults[0].Mode);
        Assert.Equal("Default", body.Defaults[0].Label);
        Assert.Null(body.Defaults[0].ModelId);
        Assert.Equal(AgentModeIds.Orchestration, body.Defaults[1].Mode);
        Assert.Equal(AgentModeIds.Review, body.Defaults[2].Mode);
        Assert.Equal(AgentModeIds.Implementation, body.Defaults[3].Mode);
        Assert.All(body.Defaults, row => Assert.Null(row.ModelId));
    }

    [Fact]
    public async Task UpdateAgentModeModelDefault_WithEnabledModel_Persists()
    {
        await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request("claude-4.6-sonnet-medium-thinking"));

        HttpResponseMessage patchResponse = await _client.PatchAsJsonAsync(
            $"/agents/cursor/mode-model-defaults/{AgentModeIds.Orchestration}",
            new UpdateAgentModeModelDefault.Request("claude-4.6-sonnet-medium-thinking"));

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        UpdateAgentModeModelDefault.DefaultResponse? patched =
            await patchResponse.Content.ReadFromJsonAsync<UpdateAgentModeModelDefault.DefaultResponse>();

        Assert.NotNull(patched);
        Assert.Equal(AgentModeIds.Orchestration, patched.Mode);
        Assert.Equal("claude-4.6-sonnet-medium-thinking", patched.ModelId);

        HttpResponseMessage listResponse = await _client.GetAsync("/agents/cursor/mode-model-defaults");
        ListAgentModeModelDefaults.Response? listBody =
            await listResponse.Content.ReadFromJsonAsync<ListAgentModeModelDefaults.Response>();

        Assert.NotNull(listBody);
        ListAgentModeModelDefaults.DefaultResponse? orchestration = listBody.Defaults
            .FirstOrDefault(row => row.Mode == AgentModeIds.Orchestration);

        Assert.NotNull(orchestration);
        Assert.Equal("claude-4.6-sonnet-medium-thinking", orchestration.ModelId);
    }

    [Fact]
    public async Task UpdateAgentModeModelDefault_WithUnknownModel_ReturnsValidationError()
    {
        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"/agents/cursor/mode-model-defaults/{AgentModeIds.Default}",
            new UpdateAgentModeModelDefault.Request("nonexistent-model"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateAgentModeModelDefault_WithDisabledModel_ReturnsValidationError()
    {
        await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request("gpt-5.3-codex"));

        await _client.PatchAsJsonAsync(
            "/agents/cursor/models/gpt-5.3-codex",
            new Orchi.Api.Features.Agents.UpdateAgentModel.UpdateAgentModel.Request(false));

        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"/agents/cursor/mode-model-defaults/{AgentModeIds.Review}",
            new UpdateAgentModeModelDefault.Request("gpt-5.3-codex"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ListAgentModeModelDefaults_UnsupportedAgent_ReturnsValidationError()
    {
        HttpResponseMessage response = await _client.GetAsync("/agents/unknown/mode-model-defaults");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
