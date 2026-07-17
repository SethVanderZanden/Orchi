using System.Net;
using System.Net.Http.Json;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class OrchestrationKickOffAllEndpointTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly string _workspacePath;

    public OrchestrationKickOffAllEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        factory.InitializeDatabase();
        _client = factory.CreateClient();
        _workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-orch-kickoff-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workspacePath);
    }

    public async Task InitializeAsync()
    {
        await _client.PostAsync("/chats/shutdown", content: null);
        await _factory.ClearAllChatsAsync();
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_workspacePath))
        {
            Directory.Delete(_workspacePath, recursive: true);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task GetOrchestration_ReturnsParsedPlansAndIdleWorkflow()
    {
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client, _workspacePath);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor", OrchestrationAgentModeStrategy.Mode));

        CreateChatResponse? parent = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(parent);

        HttpResponseMessage orchestrationResponse = await _client.GetAsync($"/chats/{parent.Id}/orchestration");
        Assert.Equal(HttpStatusCode.OK, orchestrationResponse.StatusCode);

        OrchestrationStateResponse? state =
            await orchestrationResponse.Content.ReadFromJsonAsync<OrchestrationStateResponse>();

        Assert.NotNull(state);
        Assert.Equal("idle", state.Status);
        Assert.Empty(state.Plans);
    }

    [Fact]
    public async Task KickOffAll_OnDefaultChat_ReturnsValidationError()
    {
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client, _workspacePath);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor"));

        CreateChatResponse? chat = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(chat);

        HttpResponseMessage kickoffAllResponse =
            await _client.PostAsync($"/chats/{chat.Id}/orchestration/kickoff-all", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, kickoffAllResponse.StatusCode);
    }
}
