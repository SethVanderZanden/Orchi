using System.Net;
using System.Net.Http.Json;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class KickOffPlanEndpointTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly string _workspacePath;

    public KickOffPlanEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        factory.InitializeDatabase();
        _client = factory.CreateClient();
        _workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-kickoff-{Guid.NewGuid():N}");
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
    public async Task KickOffPlan_CreatesPlanFileAndChildChat()
    {
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest("cursor", _workspacePath, OrchestrationAgentModeStrategy.Mode));

        CreateChatResponse? parent = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(parent);

        HttpResponseMessage kickoffResponse = await _client.PostAsJsonAsync(
            $"/chats/{parent.Id}/plans/kickoff",
            new KickOffPlanRequest(
                "auth-refactor",
                "Auth refactor",
                "# Auth refactor\n\nImplement JWT auth."));

        Assert.Equal(HttpStatusCode.Created, kickoffResponse.StatusCode);

        KickOffPlanResponse? kickedOff = await kickoffResponse.Content.ReadFromJsonAsync<KickOffPlanResponse>();
        Assert.NotNull(kickedOff);
        Assert.Equal(".orchi/plan-auth-refactor.md", kickedOff.PlanFilePath);
        Assert.Contains(".orchi/plan-auth-refactor.md", kickedOff.InitialPrompt);

        string planFile = Path.Combine(_workspacePath, ".orchi", "plan-auth-refactor.md");
        Assert.True(File.Exists(planFile));

        HttpResponseMessage childResponse = await _client.GetAsync($"/chats/{kickedOff.ChildChatId}");
        Assert.Equal(HttpStatusCode.OK, childResponse.StatusCode);

        ChatDetailResponse? child = await childResponse.Content.ReadFromJsonAsync<ChatDetailResponse>();
        Assert.NotNull(child);
        Assert.Equal(DefaultAgentModeStrategy.Mode, child.Mode);
        Assert.Equal(parent.Id, child.ParentChatId);
        Assert.Equal(".orchi/plan-auth-refactor.md", child.PlanFilePath);
    }

    [Fact]
    public async Task KickOffPlan_OnDefaultChat_ReturnsValidationError()
    {
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest("cursor", _workspacePath));

        CreateChatResponse? parent = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(parent);

        HttpResponseMessage kickoffResponse = await _client.PostAsJsonAsync(
            $"/chats/{parent.Id}/plans/kickoff",
            new KickOffPlanRequest("auth-refactor", "Auth refactor", "# Plan"));

        Assert.Equal(HttpStatusCode.BadRequest, kickoffResponse.StatusCode);
    }
}
