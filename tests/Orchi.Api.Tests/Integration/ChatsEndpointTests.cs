using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class ChatsEndpointTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ChatsEndpointTests(TestWebApplicationFactory factory)
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
    public async Task ListChats_Initially_ReturnsEmptyArray()
    {
        HttpResponseMessage response = await _client.GetAsync("/chats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ChatSummaryResponse[]? chats = await response.Content.ReadFromJsonAsync<ChatSummaryResponse[]>();
        Assert.NotNull(chats);
        Assert.Empty(chats);
    }

    [Fact]
    public async Task CreateChat_ThenList_ReturnsCreatedChat()
    {
        string workspace = Directory.GetCurrentDirectory();

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest("cursor", workspace));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateChatResponse? created = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(created);
        Assert.Equal("cursor", created.AgentId);

        HttpResponseMessage listResponse = await _client.GetAsync("/chats");
        ChatSummaryResponse[]? chats = await listResponse.Content.ReadFromJsonAsync<ChatSummaryResponse[]>();

        Assert.NotNull(chats);
        Assert.Single(chats);
        Assert.Equal(created.Id, chats[0].Id);
    }

    [Fact]
    public async Task CloseChat_RemovesChatFromList()
    {
        string workspace = Directory.GetCurrentDirectory();

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest("cursor", workspace));

        CreateChatResponse? created = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(created);

        HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/chats/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        ChatSummaryResponse[]? chats =
            await (await _client.GetAsync("/chats")).Content.ReadFromJsonAsync<ChatSummaryResponse[]>();

        Assert.NotNull(chats);
        Assert.Empty(chats);
    }

    [Fact]
    public async Task Shutdown_PreservesPersistedChats()
    {
        string workspace = Directory.GetCurrentDirectory();

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest("cursor", workspace));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateChatResponse? created = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(created);

        HttpResponseMessage shutdownResponse = await _client.PostAsync("/chats/shutdown", content: null);
        Assert.Equal(HttpStatusCode.NoContent, shutdownResponse.StatusCode);

        ChatSummaryResponse[]? chats =
            await (await _client.GetAsync("/chats")).Content.ReadFromJsonAsync<ChatSummaryResponse[]>();

        Assert.NotNull(chats);
        Assert.Single(chats);
        Assert.Equal(created.Id, chats[0].Id);

        HttpResponseMessage getResponse = await _client.GetAsync($"/chats/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        ChatDetailResponse? detail = await getResponse.Content.ReadFromJsonAsync<ChatDetailResponse>();
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail.Id);
    }
}
