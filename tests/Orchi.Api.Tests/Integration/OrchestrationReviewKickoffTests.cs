using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Orchestration;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class OrchestrationReviewKickoffTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly string _workspacePath;
    private Guid _workspaceId;

    public OrchestrationReviewKickoffTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        factory.InitializeDatabase();
        _client = factory.CreateClient();
        _workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-orch-review-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workspacePath);
    }

    public async Task InitializeAsync()
    {
        await _client.PostAsync("/chats/shutdown", content: null);
        await _factory.ClearAllChatsAsync();
        _workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client, _workspacePath);
    }

    public Task DisposeAsync()
    {
        if (!Directory.Exists(_workspacePath))
        {
            return Task.CompletedTask;
        }

        try
        {
            Directory.Delete(_workspacePath, recursive: true);
        }
        catch (IOException)
        {
            // Temp workspace may still be held by the API process on Windows.
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task OnImplementationTurnCompleted_AutoCreatesReviewChildInOrchestrationSnapshot()
    {
        Guid parentId = await CreateOrchestrationParentAsync();
        Guid implementationChildId = await KickOffImplementationAsync(parentId);

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IOrchestrationWorkflowService workflow =
                scope.ServiceProvider.GetRequiredService<IOrchestrationWorkflowService>();

            await workflow.OnAgentTurnCompletedAsync(implementationChildId, succeeded: true, CancellationToken.None);
        }

        HttpResponseMessage orchestrationResponse = await _client.GetAsync($"/chats/{parentId}/orchestration");
        Assert.Equal(HttpStatusCode.OK, orchestrationResponse.StatusCode);

        OrchestrationStateResponse? state =
            await orchestrationResponse.Content.ReadFromJsonAsync<OrchestrationStateResponse>();

        Assert.NotNull(state);
        Assert.Contains(
            state.Children,
            child =>
                child.Mode == ReviewAgentModeStrategy.Mode &&
                child.PlanId == "auth-refactor" &&
                string.Equals(child.PlanFilePath, ".orchi/review-auth-refactor.md", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<Guid> CreateOrchestrationParentAsync()
    {
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(_workspaceId, "cursor", OrchestrationAgentModeStrategy.Mode));

        CreateChatResponse? parent = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(parent);

        return parent.Id;
    }

    private async Task<Guid> KickOffImplementationAsync(Guid parentId)
    {
        HttpResponseMessage kickoffResponse = await _client.PostAsJsonAsync(
            $"/chats/{parentId}/plans/kickoff",
            new KickOffPlanRequest(
                "auth-refactor",
                "Auth refactor",
                "# Auth refactor\n\nImplement JWT auth."));

        Assert.Equal(HttpStatusCode.Created, kickoffResponse.StatusCode);

        KickOffPlanResponse? kickedOff = await kickoffResponse.Content.ReadFromJsonAsync<KickOffPlanResponse>();
        Assert.NotNull(kickedOff);

        return kickedOff.ChildChatId;
    }
}
