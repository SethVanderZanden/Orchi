using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Features.Projects.Shared;
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

        ChatSummaryResponse[]? chats =
            await response.Content.ReadFromJsonAsync<ChatSummaryResponse[]>(HttpResponseExtensions.JsonOptions);
        Assert.NotNull(chats);
        Assert.Empty(chats);
    }

    [Fact]
    public async Task CreateChat_ThenList_ReturnsCreatedChat()
    {
        string workspace = Directory.GetCurrentDirectory();
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client, workspace);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateChatResponse? created = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(created);
        Assert.Equal("cursor", created.AgentId);
        Assert.Equal(workspaceId, created.WorkspaceId);
        Assert.NotNull(created.ProjectId);

        HttpResponseMessage listResponse = await _client.GetAsync("/chats");
        ChatSummaryResponse[]? chats =
            await listResponse.Content.ReadFromJsonAsync<ChatSummaryResponse[]>(HttpResponseExtensions.JsonOptions);

        Assert.NotNull(chats);
        Assert.Single(chats);
        Assert.Equal(created.Id, chats[0].Id);
        Assert.Equal(created.ProjectId, chats[0].ProjectId);
        Assert.Equal(created.WorkspaceId, chats[0].WorkspaceId);
    }

    [Fact]
    public async Task CloseChat_RemovesChatFromList()
    {
        string workspace = Directory.GetCurrentDirectory();
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client, workspace);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor"));

        CreateChatResponse? created = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(created);

        HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/chats/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        ChatSummaryResponse[]? chats =
            await (await _client.GetAsync("/chats")).Content.ReadFromJsonAsync<ChatSummaryResponse[]>(
                HttpResponseExtensions.JsonOptions);

        Assert.NotNull(chats);
        Assert.Empty(chats);
    }

    [Fact]
    public async Task BulkCloseChats_RemovesSelectedChatsAndPreservesProjects()
    {
        string workspace = Directory.GetCurrentDirectory();
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client, workspace);

        HttpResponseMessage firstCreate = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor"));
        HttpResponseMessage secondCreate = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor"));
        HttpResponseMessage keepCreate = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor"));

        CreateChatResponse? first = await firstCreate.Content.ReadFromJsonAsync<CreateChatResponse>();
        CreateChatResponse? second = await secondCreate.Content.ReadFromJsonAsync<CreateChatResponse>();
        CreateChatResponse? keep = await keepCreate.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotNull(keep);

        HttpResponseMessage deleteResponse = await _client.PostAsJsonAsync(
            "/chats/bulk-delete",
            new { chatIds = new[] { first.Id, second.Id } });
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        ChatSummaryResponse[]? chats =
            await (await _client.GetAsync("/chats")).Content.ReadFromJsonAsync<ChatSummaryResponse[]>(
                HttpResponseExtensions.JsonOptions);

        Assert.NotNull(chats);
        Assert.Single(chats);
        Assert.Equal(keep.Id, chats[0].Id);

        Assert.NotNull(keep.ProjectId);
        HttpResponseMessage projectsResponse = await _client.GetAsync("/projects");
        Assert.Equal(HttpStatusCode.OK, projectsResponse.StatusCode);
        ProjectSummaryResponse[]? projects =
            await projectsResponse.Content.ReadFromJsonAsync<ProjectSummaryResponse[]>(
                HttpResponseExtensions.JsonOptions);
        Assert.NotNull(projects);
        ProjectSummaryResponse project = Assert.Single(projects, entry => entry.Id == keep.ProjectId);
        Assert.Contains(project.Workspaces, workspaceEntry => workspaceEntry.Id == workspaceId);
        Assert.Contains(project.Workspaces, workspaceEntry => workspaceEntry.Kind == "primary");
    }

    [Fact]
    public async Task BulkCloseChats_WithEmptyList_ReturnsValidationProblem()
    {
        HttpResponseMessage deleteResponse = await _client.PostAsJsonAsync(
            "/chats/bulk-delete",
            new { chatIds = Array.Empty<Guid>() });

        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Shutdown_PreservesPersistedChats()
    {
        string workspace = Directory.GetCurrentDirectory();
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client, workspace);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(workspaceId, "cursor"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateChatResponse? created = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(created);

        HttpResponseMessage shutdownResponse = await _client.PostAsync("/chats/shutdown", content: null);
        Assert.Equal(HttpStatusCode.NoContent, shutdownResponse.StatusCode);

        ChatSummaryResponse[]? chats =
            await (await _client.GetAsync("/chats")).Content.ReadFromJsonAsync<ChatSummaryResponse[]>(
                HttpResponseExtensions.JsonOptions);

        Assert.NotNull(chats);
        Assert.Single(chats);
        Assert.Equal(created.Id, chats[0].Id);

        HttpResponseMessage getResponse = await _client.GetAsync($"/chats/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        ChatDetailResponse? detail =
            await getResponse.Content.ReadFromJsonAsync<ChatDetailResponse>(HttpResponseExtensions.JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail.Id);
        Assert.Equal(created.ProjectId, detail.ProjectId);
        Assert.Equal(created.WorkspaceId, detail.WorkspaceId);
    }
}
