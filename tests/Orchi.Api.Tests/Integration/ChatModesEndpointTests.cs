using System.Net;
using System.Net.Http.Json;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class ChatModesEndpointTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ChatModesEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        factory.InitializeDatabase();
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        await _client.PostAsync("/chats/shutdown", content: null);
        await _factory.ClearAllChatsAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateImplementChat_WithoutPlan_ReturnsValidationError()
    {
        string workspace = Directory.GetCurrentDirectory();

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest("cursor", workspace, "implement"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateImplementChat_WithPlan_Succeeds()
    {
        string workspace = Directory.GetCurrentDirectory();

        HttpResponseMessage createPlanChat = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest("cursor", workspace, "plan"));

        CreateChatResponse? planChat = await createPlanChat.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(planChat);

        HttpResponseMessage planResponse = await _client.PostAsJsonAsync(
            $"/chats/{planChat.Id}/plans",
            new CreatePlanRequest("Test plan", "Step one"));

        PlanResponse? plan = await planResponse.Content.ReadFromJsonAsync<PlanResponse>();
        Assert.NotNull(plan);

        HttpResponseMessage implementChatResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest("cursor", workspace, "implement", AttachedPlanId: plan.Id));

        Assert.Equal(HttpStatusCode.Created, implementChatResponse.StatusCode);

        CreateChatResponse? implementChat = await implementChatResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(implementChat);
        Assert.Equal("implement", implementChat.Mode);
        Assert.Equal(plan.Id, implementChat.AttachedPlanId);
    }

    [Fact]
    public async Task HandoffToGoal_CreatesGoalChat()
    {
        string workspace = Directory.GetCurrentDirectory();

        HttpResponseMessage orchestratorResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest("cursor", workspace, "orchestrate"));

        CreateChatResponse? orchestrator = await orchestratorResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(orchestrator);

        HttpResponseMessage handoffResponse = await _client.PostAsync(
            $"/chats/{orchestrator.Id}/handoff-to-goal",
            content: null);

        Assert.Equal(HttpStatusCode.Created, handoffResponse.StatusCode);

        HandoffToGoalResponse? handoff = await handoffResponse.Content.ReadFromJsonAsync<HandoffToGoalResponse>();
        Assert.NotNull(handoff);
        Assert.NotEqual(Guid.Empty, handoff.GoalChatId);
    }

    [Fact]
    public async Task UpdateChatMode_ToAgent_Succeeds()
    {
        string workspace = Directory.GetCurrentDirectory();

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest("cursor", workspace, "plan"));

        CreateChatResponse? chat = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(chat);

        HttpResponseMessage updateResponse = await _client.PatchAsJsonAsync(
            $"/chats/{chat.Id}",
            new UpdateChatRequest("agent"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        ChatSummaryResponse? updated = await updateResponse.Content.ReadFromJsonAsync<ChatSummaryResponse>();
        Assert.NotNull(updated);
        Assert.Equal("agent", updated.Mode);
    }

    [Fact]
    public async Task UpdateChatMode_ToImplementWithoutPlan_ReturnsValidationError()
    {
        string workspace = Directory.GetCurrentDirectory();

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest("cursor", workspace, "agent"));

        CreateChatResponse? chat = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(chat);

        HttpResponseMessage updateResponse = await _client.PatchAsJsonAsync(
            $"/chats/{chat.Id}",
            new UpdateChatRequest("implement"));

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    }
}
