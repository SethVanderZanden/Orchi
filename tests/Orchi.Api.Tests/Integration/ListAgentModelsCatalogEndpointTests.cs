using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Orchi.Api.Features.Agents.AddAgentModel;
using Orchi.Api.Features.Agents.ListAgentModels;
using Orchi.Api.Features.Agents.RemoveAgentModel;
using Orchi.Api.Features.Agents.UpdateAgentModel;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Models;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class ListAgentModelsCatalogEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ListAgentModelsCatalogEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        factory.InitializeDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ListAgentModels_AfterManualAdd_ReturnsEnabledModel()
    {
        HttpResponseMessage addResponse = await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request("claude-4.6-sonnet-medium-thinking"));

        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        HttpResponseMessage listResponse = await _client.GetAsync("/agents/cursor/models?includeDisabled=false");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        ListAgentModels.Response? body = await listResponse.Content.ReadFromJsonAsync<ListAgentModels.Response>();
        Assert.NotNull(body);
        Assert.Contains(body.Models, model => model.Id == "claude-4.6-sonnet-medium-thinking" && model.IsEnabled);
    }

    [Fact]
    public async Task UpdateAgentModel_DisableModel_ExcludesFromEnabledList()
    {
        await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request("gpt-5.3-codex"));

        HttpResponseMessage disableResponse = await _client.PatchAsJsonAsync(
            "/agents/cursor/models",
            new UpdateAgentModel.Request("gpt-5.3-codex", false));

        Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);

        HttpResponseMessage enabledListResponse = await _client.GetAsync("/agents/cursor/models?includeDisabled=false");
        ListAgentModels.Response? enabledBody =
            await enabledListResponse.Content.ReadFromJsonAsync<ListAgentModels.Response>();

        Assert.NotNull(enabledBody);
        Assert.DoesNotContain(enabledBody.Models, model => model.Id == "gpt-5.3-codex");

        HttpResponseMessage allListResponse = await _client.GetAsync("/agents/cursor/models?includeDisabled=true");
        ListAgentModels.Response? allBody =
            await allListResponse.Content.ReadFromJsonAsync<ListAgentModels.Response>();

        Assert.NotNull(allBody);
        Assert.Contains(allBody.Models, model => model.Id == "gpt-5.3-codex" && !model.IsEnabled);
    }

    [Fact]
    public async Task RemoveAgentModel_AfterManualAdd_ExcludesFromAllLists()
    {
        await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request("gemini-2.5-pro"));

        HttpResponseMessage removeResponse = await _client.PostAsJsonAsync(
            "/agents/cursor/models/remove",
            new RemoveAgentModel.Request("gemini-2.5-pro"));

        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        HttpResponseMessage allListResponse = await _client.GetAsync("/agents/cursor/models?includeDisabled=true");
        ListAgentModels.Response? allBody =
            await allListResponse.Content.ReadFromJsonAsync<ListAgentModels.Response>();

        Assert.NotNull(allBody);
        Assert.DoesNotContain(allBody.Models, model => model.Id == "gemini-2.5-pro");
    }

    [Fact]
    public async Task RemoveAgentModel_WhenMissing_ReturnsNotFound()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/agents/cursor/models/remove",
            new RemoveAgentModel.Request("missing-model"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        // Odd catalog ids (spaces / tip text) must still delete by exact model id in the body.
        const string oddModelId =
            "Tip: use --model <id> (or %2Fmodel <id> in interactive mode) to switch.";

        await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request(oddModelId));

        HttpResponseMessage oddRemove = await _client.PostAsJsonAsync(
            "/agents/cursor/models/remove",
            new RemoveAgentModel.Request(oddModelId));

        Assert.Equal(HttpStatusCode.NoContent, oddRemove.StatusCode);
    }

    [Fact]
    public async Task ListAgentModels_UnsupportedAgent_ReturnsValidationError()
    {
        HttpResponseMessage response = await _client.GetAsync("/agents/unknown/models");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
