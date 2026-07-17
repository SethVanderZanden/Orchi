using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Orchi.Api.Entities;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class ChatStatusEndpointTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ChatStatusEndpointTests(TestWebApplicationFactory factory)
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
    public async Task CreateChat_ListIncludesReadStatus()
    {
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(
            _client,
            Directory.GetCurrentDirectory());

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        ChatSummaryResponse[]? chats =
            await (await _client.GetAsync("/chats")).Content.ReadFromJsonAsync<ChatSummaryResponse[]>(JsonOptions);

        Assert.NotNull(chats);
        Assert.Single(chats);
        Assert.Equal(ChatStatus.Read, chats[0].Status);
        Assert.Null(chats[0].LastReadAt);
    }

    [Fact]
    public async Task MarkChatRead_SetsReadAndLastReadAt()
    {
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(
            _client,
            Directory.GetCurrentDirectory());

        CreateChatResponse? created = await (await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor"))).Content.ReadFromJsonAsync<CreateChatResponse>(JsonOptions);

        Assert.NotNull(created);

        IChatStore store = _factory.Services.GetRequiredService<IChatStore>();
        ChatStatus? updated = await store.UpdateStatusAsync(
            created.Id,
            ChatStatus.ReadyForReview,
            CancellationToken.None);
        Assert.Equal(ChatStatus.ReadyForReview, updated);

        HttpResponseMessage markResponse = await _client.PostAsync($"/chats/{created.Id}/read", content: null);
        Assert.Equal(HttpStatusCode.OK, markResponse.StatusCode);

        ChatSummaryResponse? summary =
            await markResponse.Content.ReadFromJsonAsync<ChatSummaryResponse>(JsonOptions);

        Assert.NotNull(summary);
        Assert.Equal(ChatStatus.Read, summary.Status);
        Assert.NotNull(summary.LastReadAt);

        ChatSummaryResponse[]? chats =
            await (await _client.GetAsync("/chats")).Content.ReadFromJsonAsync<ChatSummaryResponse[]>(JsonOptions);

        Assert.NotNull(chats);
        Assert.Equal(ChatStatus.Read, chats[0].Status);
    }

    [Fact]
    public async Task MarkChatRead_WhenIdleInProgress_SetsRead()
    {
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(
            _client,
            Directory.GetCurrentDirectory());

        CreateChatResponse? created = await (await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor"))).Content.ReadFromJsonAsync<CreateChatResponse>(JsonOptions);

        Assert.NotNull(created);

        IChatStore store = _factory.Services.GetRequiredService<IChatStore>();
        await store.UpdateStatusAsync(created.Id, ChatStatus.InProgress, CancellationToken.None);

        HttpResponseMessage markResponse = await _client.PostAsync($"/chats/{created.Id}/read", content: null);
        Assert.Equal(HttpStatusCode.OK, markResponse.StatusCode);

        ChatSummaryResponse? summary =
            await markResponse.Content.ReadFromJsonAsync<ChatSummaryResponse>(JsonOptions);

        Assert.NotNull(summary);
        Assert.Equal(ChatStatus.Read, summary.Status);
        Assert.NotNull(summary.LastReadAt);
    }

    [Fact]
    public async Task MarkChatRead_WhileSessionRunning_KeepsInProgress()
    {
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(
            _client,
            Directory.GetCurrentDirectory());

        CreateChatResponse? created = await (await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor"))).Content.ReadFromJsonAsync<CreateChatResponse>(JsonOptions);

        Assert.NotNull(created);

        AgentSessionManager sessions = _factory.Services.GetRequiredService<AgentSessionManager>();
        ChatSession? session = await sessions.GetOrLoadSessionAsync(created.Id, CancellationToken.None);
        Assert.NotNull(session);

        var runCts = new CancellationTokenSource();
        session.RunCts = runCts;
        session.Status = ChatStatus.InProgress;

        IChatStore store = _factory.Services.GetRequiredService<IChatStore>();
        await store.UpdateStatusAsync(created.Id, ChatStatus.InProgress, CancellationToken.None);

        try
        {
            HttpResponseMessage markResponse = await _client.PostAsync($"/chats/{created.Id}/read", content: null);
            Assert.Equal(HttpStatusCode.OK, markResponse.StatusCode);

            ChatSummaryResponse? summary =
                await markResponse.Content.ReadFromJsonAsync<ChatSummaryResponse>(JsonOptions);

            Assert.NotNull(summary);
            Assert.Equal(ChatStatus.InProgress, summary.Status);
            Assert.NotNull(summary.LastReadAt);
        }
        finally
        {
            session.RunCts = null;
            runCts.Dispose();
        }
    }

    [Fact]
    public async Task StatusEvents_SnapshotIncludesChats()
    {
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(
            _client,
            Directory.GetCurrentDirectory());

        CreateChatResponse? created = await (await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor"))).Content.ReadFromJsonAsync<CreateChatResponse>(JsonOptions);

        Assert.NotNull(created);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        using HttpResponseMessage response = await _client.GetAsync(
            "/chats/status/events",
            HttpCompletionOption.ResponseHeadersRead,
            cts.Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        await using Stream stream = await response.Content.ReadAsStreamAsync(cts.Token);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? eventLine = await reader.ReadLineAsync(cts.Token);
        string? dataLine = await reader.ReadLineAsync(cts.Token);

        Assert.Equal("event: snapshot", eventLine);
        Assert.NotNull(dataLine);
        Assert.StartsWith("data: ", dataLine);

        string json = dataLine["data: ".Length..];
        ChatStatusSnapshotItem[]? snapshot =
            JsonSerializer.Deserialize<ChatStatusSnapshotItem[]>(json, JsonOptions);

        Assert.NotNull(snapshot);
        Assert.Contains(snapshot, item => item.ChatId == created.Id && item.Status == ChatStatus.Read);

        cts.Cancel();
    }
}
