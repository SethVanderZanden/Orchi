using System.Net;
using System.Net.Http.Json;
using Orchi.Api.Features.Projects.Shared;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Features.Projects;

public class ProjectsEndpointTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProjectsEndpointTests(TestWebApplicationFactory factory)
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
    public async Task CreateProject_ReturnsProjectWithDefaultWorkspace()
    {
        string workspacePath = Directory.GetCurrentDirectory();

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/projects",
            new CreateProjectRequest("Orchi", workspacePath));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        CreateProjectResponse? created = await response.Content.ReadFromJsonAsync<CreateProjectResponse>();
        Assert.NotNull(created);
        Assert.Equal("Orchi", created.Name);
        Assert.True(created.DefaultWorkspace.IsDefault);
        Assert.Equal("Primary", created.DefaultWorkspace.Kind);
    }

    [Fact]
    public async Task ListProjects_IncludesWorkspaces()
    {
        string workspacePath = Directory.GetCurrentDirectory();
        await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client, workspacePath, "Listed Project");

        HttpResponseMessage response = await _client.GetAsync("/projects");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ProjectSummaryResponse[]? projects =
            await response.Content.ReadFromJsonAsync<ProjectSummaryResponse[]>();

        Assert.NotNull(projects);
        Assert.NotEmpty(projects);
        Assert.All(projects, project => Assert.NotEmpty(project.Workspaces));
    }

    [Fact]
    public async Task UpdateProject_RenamesProject()
    {
        string workspacePath = Directory.GetCurrentDirectory();

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/projects",
            new CreateProjectRequest("Old Name", workspacePath));

        CreateProjectResponse? created = await createResponse.Content.ReadFromJsonAsync<CreateProjectResponse>();
        Assert.NotNull(created);

        HttpResponseMessage updateResponse = await _client.PatchAsJsonAsync(
            $"/projects/{created.Id}",
            new UpdateProjectRequest("New Name"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        ProjectDetailResponse? updated = await updateResponse.Content.ReadFromJsonAsync<ProjectDetailResponse>();
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task CreateWorkspace_AddsNonDefaultWorkspace()
    {
        string primaryPath = Directory.GetCurrentDirectory();
        string secondaryPath = Path.Combine(Path.GetTempPath(), $"orchi-secondary-{Guid.NewGuid():N}");
        Directory.CreateDirectory(secondaryPath);

        try
        {
            HttpResponseMessage createProject = await _client.PostAsJsonAsync(
                "/projects",
                new CreateProjectRequest("Multi", primaryPath));

            CreateProjectResponse? project = await createProject.Content.ReadFromJsonAsync<CreateProjectResponse>();
            Assert.NotNull(project);

            HttpResponseMessage createWorkspace = await _client.PostAsJsonAsync(
                $"/projects/{project.Id}/workspaces",
                new CreateWorkspaceRequest(secondaryPath, "Secondary"));

            Assert.Equal(HttpStatusCode.Created, createWorkspace.StatusCode);

            WorkspaceResponse? workspace = await createWorkspace.Content.ReadFromJsonAsync<WorkspaceResponse>();
            Assert.NotNull(workspace);
            Assert.False(workspace.IsDefault);
            Assert.Equal("Secondary", workspace.Name);
        }
        finally
        {
            if (Directory.Exists(secondaryPath))
            {
                Directory.Delete(secondaryPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UpdateWorkspace_SetsDefault()
    {
        string primaryPath = Directory.GetCurrentDirectory();
        string secondaryPath = Path.Combine(Path.GetTempPath(), $"orchi-default-{Guid.NewGuid():N}");
        Directory.CreateDirectory(secondaryPath);

        try
        {
            HttpResponseMessage createProject = await _client.PostAsJsonAsync(
                "/projects",
                new CreateProjectRequest("Default Switch", primaryPath));

            CreateProjectResponse? project = await createProject.Content.ReadFromJsonAsync<CreateProjectResponse>();
            Assert.NotNull(project);

            HttpResponseMessage createWorkspace = await _client.PostAsJsonAsync(
                $"/projects/{project.Id}/workspaces",
                new CreateWorkspaceRequest(secondaryPath));

            WorkspaceResponse? secondary = await createWorkspace.Content.ReadFromJsonAsync<WorkspaceResponse>();
            Assert.NotNull(secondary);

            HttpResponseMessage updateResponse = await _client.PatchAsJsonAsync(
                $"/workspaces/{secondary.Id}",
                new UpdateWorkspaceRequest(IsDefault: true));

            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            WorkspaceResponse? updated = await updateResponse.Content.ReadFromJsonAsync<WorkspaceResponse>();
            Assert.NotNull(updated);
            Assert.True(updated.IsDefault);
        }
        finally
        {
            if (Directory.Exists(secondaryPath))
            {
                Directory.Delete(secondaryPath, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DeleteLastWorkspace_ReturnsValidationError()
    {
        string workspacePath = Directory.GetCurrentDirectory();
        Guid workspaceId = await ProjectTestHelper.CreateProjectWithWorkspaceAsync(_client, workspacePath);

        HttpResponseMessage deleteResponse = await _client.DeleteAsync($"/workspaces/{workspaceId}");
        Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);
    }
}
