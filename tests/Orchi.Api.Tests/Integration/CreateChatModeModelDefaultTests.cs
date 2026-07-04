using System.Net;
using System.Net.Http.Json;
using Orchi.Api.Features.Agents.AddAgentModel;
using Orchi.Api.Features.Agents.UpdateAgentModeModelDefault;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class CreateChatModeModelDefaultTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CreateChatModeModelDefaultTests(TestWebApplicationFactory factory)
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
    public async Task CreateChat_AppliesDefaultModeModelSetting()
    {
        await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request("claude-4.6-sonnet-medium-thinking"));

        HttpResponseMessage patchResponse = await _client.PatchAsJsonAsync(
            $"/agents/cursor/mode-model-defaults/{AgentModeIds.Default}",
            new UpdateAgentModeModelDefault.Request("claude-4.6-sonnet-medium-thinking"));

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        string workspace = Directory.GetCurrentDirectory();
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client, workspace);

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest("cursor", workspaceId));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        CreateChatResponse? created = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(created);
        Assert.Equal("claude-4.6-sonnet-medium-thinking", created.ModelId);
    }

    [Fact]
    public async Task KickOffPlan_ChildUsesImplementationModeDefault_NotParentModel()
    {
        await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request("gpt-5.3-codex"));

        await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request("claude-4.6-sonnet-medium-thinking"));

        await _client.PatchAsJsonAsync(
            $"/agents/cursor/mode-model-defaults/{AgentModeIds.Implementation}",
            new UpdateAgentModeModelDefault.Request("gpt-5.3-codex"));

        string workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-mode-kickoff-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspacePath);

        try
        {
            Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client, workspacePath);

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
                "/chats",
                new CreateChatRequest("cursor", workspaceId, OrchestrationAgentModeStrategy.Mode));

            CreateChatResponse? parent = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
            Assert.NotNull(parent);

            HttpResponseMessage patchParentModel = await _client.PatchAsJsonAsync(
                $"/chats/{parent.Id}/model",
                new UpdateChatModelRequest("claude-4.6-sonnet-medium-thinking"));

            Assert.Equal(HttpStatusCode.OK, patchParentModel.StatusCode);

            HttpResponseMessage kickoffResponse = await _client.PostAsJsonAsync(
                $"/chats/{parent.Id}/plans/kickoff",
                new KickOffPlanRequest(
                    "mode-default-test",
                    "Mode default test",
                    "# Plan\n\nTest implementation default."));

            Assert.Equal(HttpStatusCode.Created, kickoffResponse.StatusCode);

            KickOffPlanResponse? kickedOff = await kickoffResponse.Content.ReadFromJsonAsync<KickOffPlanResponse>();
            Assert.NotNull(kickedOff);

            HttpResponseMessage childResponse = await _client.GetAsync($"/chats/{kickedOff.ChildChatId}");
            ChatDetailResponse? child = await childResponse.Content.ReadFromJsonAsync<ChatDetailResponse>();

            Assert.NotNull(child);
            Assert.Equal("gpt-5.3-codex", child.ModelId);
        }
        finally
        {
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task KickOffReview_ChildUsesReviewModeDefault_NotParentModel()
    {
        await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request("gpt-5.3-codex"));

        await _client.PostAsJsonAsync(
            "/agents/cursor/models",
            new AddAgentModel.Request("composer-2.5-fast"));

        await _client.PatchAsJsonAsync(
            $"/agents/cursor/mode-model-defaults/{AgentModeIds.Review}",
            new UpdateAgentModeModelDefault.Request("composer-2.5-fast"));

        string workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-review-default-{Guid.NewGuid():N}");
        Directory.CreateDirectory(workspacePath);

        try
        {
            Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client, workspacePath);

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
                "/chats",
                new CreateChatRequest("cursor", workspaceId, OrchestrationAgentModeStrategy.Mode));

            CreateChatResponse? parent = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
            Assert.NotNull(parent);

            await _client.PatchAsJsonAsync(
                $"/chats/{parent.Id}/model",
                new UpdateChatModelRequest("gpt-5.3-codex"));

            HttpResponseMessage kickoffResponse = await _client.PostAsJsonAsync(
                $"/chats/{parent.Id}/plans/kickoff",
                new KickOffPlanRequest(
                    "review-default-test",
                    "Review default test",
                    "# Plan\n\nTest review default."));

            KickOffPlanResponse? kickedOff = await kickoffResponse.Content.ReadFromJsonAsync<KickOffPlanResponse>();
            Assert.NotNull(kickedOff);

            HttpResponseMessage reviewResponse = await _client.PostAsync(
                $"/chats/{kickedOff.ChildChatId}/review/kickoff",
                content: null);

            Assert.Equal(HttpStatusCode.Created, reviewResponse.StatusCode);

            KickOffReviewResponse? reviewKickedOff =
                await reviewResponse.Content.ReadFromJsonAsync<KickOffReviewResponse>();

            Assert.NotNull(reviewKickedOff);

            HttpResponseMessage reviewChildResponse =
                await _client.GetAsync($"/chats/{reviewKickedOff.ReviewChildChatId}");

            ChatDetailResponse? reviewChild =
                await reviewChildResponse.Content.ReadFromJsonAsync<ChatDetailResponse>();

            Assert.NotNull(reviewChild);
            Assert.Equal("composer-2.5-fast", reviewChild.ModelId);
        }
        finally
        {
            if (Directory.Exists(workspacePath))
            {
                Directory.Delete(workspacePath, recursive: true);
            }
        }
    }
}
