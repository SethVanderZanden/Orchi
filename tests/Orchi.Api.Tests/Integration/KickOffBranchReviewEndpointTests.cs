using System.Net;
using System.Net.Http.Json;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Features.Projects.CreateWorktree;
using Orchi.Api.Features.Projects.Shared;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class KickOffBranchReviewEndpointTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly string _workspacePath;
    private Guid _projectId;
    private Guid _workspaceId;

    public KickOffBranchReviewEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        factory.InitializeDatabase();
        _client = factory.CreateClient();
        _workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-branch-review-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workspacePath);
    }

    public async Task InitializeAsync()
    {
        await _client.PostAsync("/chats/shutdown", content: null);
        await _factory.ClearAllChatsAsync();
        InitializeGitRepoWithFeatureBranch();

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/projects",
            new CreateProjectRequest("Branch Review Project", _workspacePath));

        CreateProjectResponse? created = await response.Content.ReadFromJsonAsync<CreateProjectResponse>();
        Assert.NotNull(created);
        _projectId = created.Id;
        _workspaceId = created.DefaultWorkspace.Id;

        await _client.PatchAsJsonAsync(
            $"/projects/{_projectId}",
            new UpdateProjectRequest(UseWorktreeOnKickoff: false, DefaultBaseBranch: "main"));
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_workspacePath))
        {
            try
            {
                Directory.Delete(_workspacePath, recursive: true);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ListBranches_IncludesLocalBranches()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        HttpResponseMessage response = await _client.GetAsync($"/projects/{_projectId}/branches");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ProjectBranchResponse[]? branches =
            await response.Content.ReadFromJsonAsync<ProjectBranchResponse[]>();
        Assert.NotNull(branches);
        Assert.Contains(branches, branch => branch.Name is "main" or "master");
        Assert.Contains(branches, branch => branch.Name == "feature-auth");
    }

    [Fact]
    public async Task KickOffBranchReview_CreatesReviewChatAndBrief()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/projects/{_projectId}/reviews/from-branches",
            new KickOffBranchReviewRequest("feature-auth", _workspaceId, "main", Fetch: false));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        KickOffBranchReviewResponse? kickedOff =
            await response.Content.ReadFromJsonAsync<KickOffBranchReviewResponse>();
        Assert.NotNull(kickedOff);
        Assert.Equal("Begin review.", kickedOff.KickoffMessage);
        Assert.Contains("review-branch-feature-auth", kickedOff.ReviewFilePath);
        Assert.Equal("feature-auth", kickedOff.HeadBranch);
        Assert.Equal("main", kickedOff.BaseBranch);

        HttpResponseMessage chatResponse = await _client.GetAsync($"/chats/{kickedOff.ReviewChatId}");
        Assert.Equal(HttpStatusCode.OK, chatResponse.StatusCode);

        ChatDetailResponse? reviewChat = await chatResponse.Content.ReadFromJsonAsync<ChatDetailResponse>(
            HttpResponseExtensions.JsonOptions);
        Assert.NotNull(reviewChat);
        Assert.Equal(ReviewAgentModeStrategy.Mode, reviewChat.Mode);
        Assert.Equal(kickedOff.ReviewFilePath, reviewChat.PlanFilePath);
        Assert.Equal(_workspacePath, reviewChat.WorkspacePath);

        string reviewFile = Path.Combine(
            _workspacePath,
            kickedOff.ReviewFilePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(reviewFile));
        string content = await File.ReadAllTextAsync(reviewFile);
        Assert.Contains("orchi-branch-review", content);
        Assert.Contains("feature-auth", content);
        Assert.Contains("main", content);
    }

    [Fact]
    public async Task KickOffBranchReview_WithWorkspaceId_UsesRequestedWorkspace()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        HttpResponseMessage worktreeResponse = await _client.PostAsJsonAsync(
            $"/projects/{_projectId}/worktrees",
            new CreateWorktreeRequest("main", "feature-auth", "Review source worktree"));

        Assert.Equal(HttpStatusCode.Created, worktreeResponse.StatusCode);

        WorkspaceResponse? worktreeWorkspace =
            await worktreeResponse.Content.ReadFromJsonAsync<WorkspaceResponse>();
        Assert.NotNull(worktreeWorkspace);

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/projects/{_projectId}/reviews/from-branches",
            new KickOffBranchReviewRequest(
                "feature-auth",
                worktreeWorkspace.Id,
                "main",
                Fetch: false));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        KickOffBranchReviewResponse? kickedOff =
            await response.Content.ReadFromJsonAsync<KickOffBranchReviewResponse>();
        Assert.NotNull(kickedOff);

        HttpResponseMessage chatResponse = await _client.GetAsync($"/chats/{kickedOff.ReviewChatId}");
        ChatDetailResponse? reviewChat = await chatResponse.Content.ReadFromJsonAsync<ChatDetailResponse>(
            HttpResponseExtensions.JsonOptions);
        Assert.NotNull(reviewChat);
        Assert.Equal(worktreeWorkspace.Id, reviewChat.WorkspaceId);
        Assert.Equal(worktreeWorkspace.Path, reviewChat.WorkspacePath);
        Assert.NotEqual(_workspacePath, reviewChat.WorkspacePath);
    }

    [Fact]
    public async Task KickOffBranchReview_SameBranches_ReturnsValidationError()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/projects/{_projectId}/reviews/from-branches",
            new KickOffBranchReviewRequest("main", _workspaceId, "main", Fetch: false));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private void InitializeGitRepoWithFeatureBranch()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        RunGit("init");
        RunGit("checkout", "-b", "main");
        File.WriteAllText(Path.Combine(_workspacePath, "readme.txt"), "base\n");
        RunGit("add", "readme.txt");
        RunGit("-c", "user.email=test@example.com", "-c", "user.name=Test", "commit", "-m", "init");
        RunGit("checkout", "-b", "feature-auth");
        File.WriteAllText(Path.Combine(_workspacePath, "auth.txt"), "auth\n");
        RunGit("add", "auth.txt");
        RunGit("-c", "user.email=test@example.com", "-c", "user.name=Test", "commit", "-m", "feature");
        RunGit("checkout", "main");
    }

    private void RunGit(params string[] args)
    {
        using var process = new System.Diagnostics.Process();
        process.StartInfo.FileName = "git";
        process.StartInfo.WorkingDirectory = _workspacePath;
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
            using var process = new System.Diagnostics.Process();
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
