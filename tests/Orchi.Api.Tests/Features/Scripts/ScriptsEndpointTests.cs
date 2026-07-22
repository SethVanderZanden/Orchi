using System.Net;
using System.Net.Http.Json;
using Orchi.Api.Features.Scripts.Shared;
using Orchi.Api.Infrastructure.Scripts;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Features.Scripts;

public class ScriptsEndpointTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ScriptsEndpointTests(TestWebApplicationFactory factory)
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
    public async Task CreateAndListScript_ReturnsBindings()
    {
        string stepsJson = ScriptStepsSerializer.Serialize(
        [
            new ScriptStepDto(ScriptStepKinds.Shell, Command: "npm run lint")
        ]);

        HttpResponseMessage create = await _client.PostAsJsonAsync(
            "/scripts",
            new CreateScriptRequest(
                "Lint finish",
                ProjectId: null,
                stepsJson,
                [
                    new ScriptBindingRequest("agentFinish", "implementation", Order: 0, Enabled: true, "continue")
                ]));

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        ScriptResponse? created = await create.Content.ReadFromJsonAsync<ScriptResponse>();
        Assert.NotNull(created);
        Assert.Equal("Lint finish", created.Name);
        Assert.Single(created.Bindings);
        Assert.Equal("agentFinish", created.Bindings[0].Event);
        Assert.Equal("implementation", created.Bindings[0].ModeFilter);

        HttpResponseMessage list = await _client.GetAsync("/scripts");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        ScriptResponse[]? scripts = await list.Content.ReadFromJsonAsync<ScriptResponse[]>();
        Assert.NotNull(scripts);
        Assert.Contains(scripts, script => script.Id == created.Id);
    }

    [Fact]
    public async Task ApplyOrchestrationGitDefaults_CreatesFinishScript()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/scripts/templates/orchestration-git-defaults",
            new { projectId = (Guid?)null });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        ScriptResponse[]? scripts = await response.Content.ReadFromJsonAsync<ScriptResponse[]>();
        Assert.NotNull(scripts);
        Assert.Equal(2, scripts.Length);

        ScriptResponse finishScript = scripts[1];
        Assert.Contains("git.commit", finishScript.StepsJson, StringComparison.Ordinal);
        Assert.Contains("git.push", finishScript.StepsJson, StringComparison.Ordinal);
        Assert.Contains("git.createPullRequest", finishScript.StepsJson, StringComparison.Ordinal);
        Assert.Equal("agentFinish", finishScript.Bindings[0].Event);
        Assert.Equal("implementation", finishScript.Bindings[0].ModeFilter);
    }

    [Fact]
    public async Task CreateScript_RejectsUnknownStepKind()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/scripts",
            new CreateScriptRequest(
                "Bad",
                null,
                """[{"kind":"not.a.kind"}]""",
                null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
