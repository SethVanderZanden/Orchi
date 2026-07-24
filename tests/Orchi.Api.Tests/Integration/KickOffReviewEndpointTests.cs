using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Features.Projects.Shared;
using Orchi.Api.Infrastructure.Agents;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Infrastructure.Agents.Persistence;
using Orchi.Api.Infrastructure.Projects;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class KickOffReviewEndpointTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly string _workspacePath;
    private Guid _workspaceId;

    public KickOffReviewEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        factory.InitializeDatabase();
        _client = factory.CreateClient();
        _workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-review-kickoff-{Guid.NewGuid():N}");
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
        if (Directory.Exists(_workspacePath))
        {
            Directory.Delete(_workspacePath, recursive: true);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task KickOffReview_CreatesReviewFileAndReviewChild()
    {
        Guid parentId = await CreateOrchestrationParentAsync();
        Guid implementationChildId = await KickOffImplementationAsync(parentId);

        HttpResponseMessage reviewResponse = await _client.PostAsync(
            $"/chats/{implementationChildId}/review/kickoff",
            content: null);

        Assert.Equal(HttpStatusCode.Created, reviewResponse.StatusCode);

        KickOffReviewResponse? kickedOff = await reviewResponse.Content.ReadFromJsonAsync<KickOffReviewResponse>();
        Assert.NotNull(kickedOff);
        Assert.Equal(".orchi/review-auth-refactor.md", kickedOff.ReviewFilePath);
        Assert.Contains(".orchi/review-auth-refactor.md", kickedOff.InitialPrompt);
        Assert.Contains("delete `.orchi/review-auth-refactor.md`", kickedOff.InitialPrompt);
        Assert.Equal("Begin review.", kickedOff.KickoffMessage);

        string reviewFile = Path.Combine(_workspacePath, ".orchi", "review-auth-refactor.md");
        Assert.True(File.Exists(reviewFile));
        string reviewContent = await File.ReadAllTextAsync(reviewFile);
        Assert.Contains("Original implementation plan", reviewContent);
        Assert.Contains("Implement JWT auth", reviewContent);

        HttpResponseMessage childResponse = await _client.GetAsync($"/chats/{kickedOff.ReviewChildChatId}");
        Assert.Equal(HttpStatusCode.OK, childResponse.StatusCode);

        ChatDetailResponse? reviewChild = await childResponse.Content.ReadFromJsonAsync<ChatDetailResponse>(
            HttpResponseExtensions.JsonOptions);
        Assert.NotNull(reviewChild);
        Assert.Equal(ReviewAgentModeStrategy.Mode, reviewChild.Mode);
        Assert.Equal(parentId, reviewChild.ParentChatId);
        Assert.Equal(".orchi/review-auth-refactor.md", reviewChild.PlanFilePath);
        Assert.Equal("Auth refactor review", reviewChild.Title);
    }

    [Fact]
    public async Task KickOffReview_DuplicateReviewChild_ReturnsValidationError()
    {
        Guid parentId = await CreateOrchestrationParentAsync();
        Guid implementationChildId = await KickOffImplementationAsync(parentId);

        HttpResponseMessage first = await _client.PostAsync(
            $"/chats/{implementationChildId}/review/kickoff",
            content: null);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        HttpResponseMessage second = await _client.PostAsync(
            $"/chats/{implementationChildId}/review/kickoff",
            content: null);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task KickOffReview_OnDefaultChat_ReturnsValidationError()
    {
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(_workspaceId, "cursor"));

        CreateChatResponse? chat = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(chat);

        HttpResponseMessage reviewResponse = await _client.PostAsync(
            $"/chats/{chat.Id}/review/kickoff",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, reviewResponse.StatusCode);
    }

    [Fact]
    public async Task KickOffReview_AfterWorktreeImplementation_UsesImplementationWorkspace()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        string worktreeWorkspacePath = Path.Combine(
            Path.GetTempPath(),
            $"orchi-review-worktree-{Guid.NewGuid():N}");
        Directory.CreateDirectory(worktreeWorkspacePath);

        try
        {
            InitializeGitRepo(worktreeWorkspacePath);

            HttpResponseMessage projectResponse = await _client.PostAsJsonAsync(
                "/projects",
                new CreateProjectRequest("Worktree Review Project", worktreeWorkspacePath));

            CreateProjectResponse? project = await projectResponse.Content.ReadFromJsonAsync<CreateProjectResponse>();
            Assert.NotNull(project);

            HttpResponseMessage patchResponse = await _client.PatchAsJsonAsync(
                $"/projects/{project.Id}",
                new UpdateProjectRequest(UseWorktreeOnKickoff: true, DefaultBaseBranch: "main"));
            Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

            Guid workspaceId = project.DefaultWorkspace.Id;

            HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
                "/chats",
                new CreateChatRequest(workspaceId, "cursor", OrchestrationAgentModeStrategy.Mode));

            CreateChatResponse? parent = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
            Assert.NotNull(parent);

            HttpResponseMessage kickoffResponse = await _client.PostAsJsonAsync(
                $"/chats/{parent.Id}/plans/kickoff",
                new KickOffPlanRequest(
                    "auth-refactor",
                    "Auth refactor",
                    "# Auth refactor\n\nImplement JWT auth."));

            Assert.Equal(HttpStatusCode.Created, kickoffResponse.StatusCode);

            KickOffPlanResponse? implementationKickedOff =
                await kickoffResponse.Content.ReadFromJsonAsync<KickOffPlanResponse>();
            Assert.NotNull(implementationKickedOff);

            HttpResponseMessage implementationResponse = await _client.GetAsync(
                $"/chats/{implementationKickedOff.ChildChatId}");
            ChatDetailResponse? implementationChild = await implementationResponse.Content.ReadFromJsonAsync<ChatDetailResponse>(
                HttpResponseExtensions.JsonOptions);
            Assert.NotNull(implementationChild);
            Assert.NotEqual(parent.WorkspaceId, implementationChild.WorkspaceId);
            Assert.NotEqual(parent.WorkspacePath, implementationChild.WorkspacePath);

            HttpResponseMessage reviewResponse = await _client.PostAsync(
                $"/chats/{implementationKickedOff.ChildChatId}/review/kickoff",
                content: null);
            Assert.Equal(HttpStatusCode.Created, reviewResponse.StatusCode);

            KickOffReviewResponse? reviewKickedOff =
                await reviewResponse.Content.ReadFromJsonAsync<KickOffReviewResponse>();
            Assert.NotNull(reviewKickedOff);

            HttpResponseMessage reviewChildResponse = await _client.GetAsync(
                $"/chats/{reviewKickedOff.ReviewChildChatId}");
            ChatDetailResponse? reviewChild = await reviewChildResponse.Content.ReadFromJsonAsync<ChatDetailResponse>(
                HttpResponseExtensions.JsonOptions);
            Assert.NotNull(reviewChild);
            Assert.Equal(implementationChild.WorkspaceId, reviewChild.WorkspaceId);
            Assert.Equal(implementationChild.WorkspacePath, reviewChild.WorkspacePath);
            Assert.NotEqual(parent.WorkspaceId, reviewChild.WorkspaceId);

            string reviewFile = Path.Combine(
                reviewChild.WorkspacePath,
                reviewKickedOff.ReviewFilePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(reviewFile));
            string reviewContent = await File.ReadAllTextAsync(reviewFile);
            Assert.Contains("orchi-branch-review", reviewContent);
            Assert.Contains("main", reviewContent);
        }
        finally
        {
            if (Directory.Exists(worktreeWorkspacePath))
            {
                try
                {
                    Directory.Delete(worktreeWorkspacePath, recursive: true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }

    [Fact]
    public async Task KickOffReview_PlanNotInDatabase_ReturnsValidationError()
    {
        Guid parentId = await CreateOrchestrationParentAsync();
        Guid implementationChildId;

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IChatStore chatStore = scope.ServiceProvider.GetRequiredService<IChatStore>();
            IProjectStore projectStore = scope.ServiceProvider.GetRequiredService<IProjectStore>();
            Entities.Workspace? workspace = await projectStore.GetWorkspaceAsync(_workspaceId, CancellationToken.None);

            ChatSession child = await chatStore.CreateAsync(
                new ChatCreateModel(
                    Guid.NewGuid(),
                    "cursor",
                    _workspacePath,
                    DefaultAgentModeStrategy.Mode,
                    parentId,
                    ".orchi/plan-auth-refactor.md",
                    workspace?.ProjectId,
                    _workspaceId),
                CancellationToken.None);

            implementationChildId = child.Id;
        }

        HttpResponseMessage reviewResponse = await _client.PostAsync(
            $"/chats/{implementationChildId}/review/kickoff",
            content: null);

        Assert.Equal(HttpStatusCode.BadRequest, reviewResponse.StatusCode);
        string body = await reviewResponse.Content.ReadAsStringAsync();
        Assert.Contains("Plan.NotFound", body);
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

    private static void InitializeGitRepo(string workspacePath)
    {
        RunGit(workspacePath, "init");
        RunGit(workspacePath, "checkout", "-b", "main");
        File.WriteAllText(Path.Combine(workspacePath, "readme.txt"), "base\n");
        RunGit(workspacePath, "add", "readme.txt");
        RunGit(
            workspacePath,
            "-c", "user.email=test@example.com",
            "-c", "user.name=Test",
            "commit", "-m", "init");
    }

    private static void RunGit(string workspacePath, params string[] args)
    {
        using var process = new Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.WorkingDirectory = workspacePath;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        foreach (string arg in args)
        {
            process.StartInfo.ArgumentList.Add(arg);
        }

        process.Start();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            string stderr = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {stderr}");
        }
    }

    private static bool IsGitAvailable()
    {
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "git";
            process.StartInfo.ArgumentList.Add("--version");
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
