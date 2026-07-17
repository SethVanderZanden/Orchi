using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class UpdateChatModeEndpointTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UpdateChatModeEndpointTests(TestWebApplicationFactory factory)
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
    public async Task UpdateChatMode_OnEmptyChat_Succeeds()
    {
        Guid chatId = await CreateChatAsync();

        HttpResponseMessage response = await PatchModeAsync(chatId, "orchestration");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        UpdateChatModeResponse? body = await response.Content.ReadFromJsonAsync<UpdateChatModeResponse>();
        Assert.NotNull(body);
        Assert.Equal(chatId, body.Id);
        Assert.Equal("orchestration", body.Mode);

        ChatDetailResponse? detail = await GetChatDetailAsync(chatId);
        Assert.NotNull(detail);
        Assert.Equal("orchestration", detail.Mode);
    }

    [Fact]
    public async Task UpdateChatMode_AfterUserMessage_Succeeds()
    {
        Guid chatId = await CreateChatAsync();

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            AgentSessionManager sessionManager = scope.ServiceProvider.GetRequiredService<AgentSessionManager>();
            await sessionManager.AppendUserMessageAsync(chatId, "Hello", CancellationToken.None);
        }

        HttpResponseMessage response = await PatchModeAsync(chatId, "orchestration");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        UpdateChatModeResponse? body = await response.Content.ReadFromJsonAsync<UpdateChatModeResponse>();
        Assert.NotNull(body);
        Assert.Equal("orchestration", body.Mode);

        ChatDetailResponse? detail = await GetChatDetailAsync(chatId);
        Assert.NotNull(detail);
        Assert.Equal("orchestration", detail.Mode);
        Assert.Single(detail.Messages);
    }

    [Fact]
    public async Task UpdateChatMode_WhileAgentRunning_ReturnsValidationError()
    {
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
                HttpResponseMessage response = await PatchModeAsync(chatId, "orchestration");

                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

                string payload = await response.Content.ReadAsStringAsync();
                using JsonDocument document = JsonDocument.Parse(payload);
                JsonElement root = document.RootElement;
                Assert.Equal("Mode.Busy", root.GetProperty("title").GetString());
                Assert.Contains(
                    "agent is running",
                    root.GetProperty("detail").GetString(),
                    StringComparison.OrdinalIgnoreCase);
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
    public async Task UpdateChatMode_UnsupportedMode_ReturnsValidationError()
    {
        Guid chatId = await CreateChatAsync();

        HttpResponseMessage response = await PatchModeAsync(chatId, "nonexistent-mode");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        string payload = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(payload);
        JsonElement root = document.RootElement;
        Assert.Equal("Mode.Unsupported", root.GetProperty("title").GetString());
    }

    private async Task<Guid> CreateChatAsync()
    {
        string workspace = Directory.GetCurrentDirectory();
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client, workspace);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateChatResponse? created = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(created);
        return created.Id;
    }

    private Task<HttpResponseMessage> PatchModeAsync(Guid chatId, string mode) =>
        _client.PatchAsJsonAsync($"/chats/{chatId}/mode", new UpdateChatModeRequest(mode));

    private async Task<ChatDetailResponse?> GetChatDetailAsync(Guid chatId)
    {
        HttpResponseMessage response = await _client.GetAsync($"/chats/{chatId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<ChatDetailResponse>();
    }
}
