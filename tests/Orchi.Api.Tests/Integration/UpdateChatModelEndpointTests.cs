using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Orchi.Api.Features.Agents.AddAgentModel;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class UpdateChatModelEndpointTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UpdateChatModelEndpointTests(TestWebApplicationFactory factory)
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
    public async Task UpdateChatModel_WithEnabledCatalogModel_Succeeds()
    {
        await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request("claude-4.6-sonnet-medium-thinking"));

        Guid chatId = await CreateChatAsync();

        HttpResponseMessage response = await PatchModelAsync(chatId, "claude-4.6-sonnet-medium-thinking");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        UpdateChatModelResponse? body = await response.Content.ReadFromJsonAsync<UpdateChatModelResponse>();
        Assert.NotNull(body);
        Assert.Equal(chatId, body.Id);
        Assert.Equal("claude-4.6-sonnet-medium-thinking", body.ModelId);

        ChatDetailResponse? detail = await GetChatDetailAsync(chatId);
        Assert.NotNull(detail);
        Assert.Equal("claude-4.6-sonnet-medium-thinking", detail.ModelId);
    }

    [Fact]
    public async Task UpdateChatModel_ClearToDefault_Succeeds()
    {
        await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request("gpt-5.3-codex"));

        Guid chatId = await CreateChatAsync();
        await PatchModelAsync(chatId, "gpt-5.3-codex");

        HttpResponseMessage response = await PatchModelAsync(chatId, null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        UpdateChatModelResponse? body = await response.Content.ReadFromJsonAsync<UpdateChatModelResponse>();
        Assert.NotNull(body);
        Assert.Null(body.ModelId);
    }

    [Fact]
    public async Task UpdateChatModel_WhileAgentRunning_ReturnsValidationError()
    {
        await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request("composer-2.5-fast"));

        Guid chatId = await CreateChatAsync();

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            AgentSessionManager sessionManager = scope.ServiceProvider.GetRequiredService<AgentSessionManager>();
            ChatSession? session = await sessionManager.GetOrLoadSessionAsync(chatId, CancellationToken.None);
            Assert.NotNull(session);

            lock (session.Sync)
            {
                session.RunningProcess = Process.GetCurrentProcess();
            }

            try
            {
                HttpResponseMessage response = await PatchModelAsync(chatId, "composer-2.5-fast");

                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

                string payload = await response.Content.ReadAsStringAsync();
                using JsonDocument document = JsonDocument.Parse(payload);
                JsonElement root = document.RootElement;
                Assert.Equal("Model.Busy", root.GetProperty("title").GetString());
            }
            finally
            {
                lock (session.Sync)
                {
                    session.RunningProcess = null;
                }
            }
        }
    }

    [Fact]
    public async Task UpdateChatModel_UnknownModel_ReturnsValidationError()
    {
        Guid chatId = await CreateChatAsync();

        HttpResponseMessage response = await PatchModelAsync(chatId, "nonexistent-model");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        string payload = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;
        Assert.Equal("Model.Unsupported", root.GetProperty("title").GetString());
    }

    private async Task<Guid> CreateChatAsync()
    {
        string workspace = Directory.GetCurrentDirectory();
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client, workspace);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest("cursor", workspaceId));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateChatResponse? created = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(created);
        return created.Id;
    }

    private Task<HttpResponseMessage> PatchModelAsync(Guid chatId, string? modelId) =>
        _client.PatchAsJsonAsync($"/chats/{chatId}/model", new UpdateChatModelRequest(modelId));

    private async Task<ChatDetailResponse?> GetChatDetailAsync(Guid chatId)
    {
        HttpResponseMessage response = await _client.GetAsync($"/chats/{chatId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ChatDetailResponse>();
    }
}
