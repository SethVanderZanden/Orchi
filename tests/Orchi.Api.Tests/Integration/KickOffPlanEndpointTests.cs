using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orchi.Api.Data;
using Orchi.Api.Entities;
using Orchi.Api.Features.Chats.Shared;
using Orchi.Api.Features.Projects.Shared;
using Orchi.Api.Infrastructure.Agents.Modes;
using Orchi.Api.Tests.Common;

namespace Orchi.Api.Tests.Integration;

public class KickOffPlanEndpointTests : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly string _workspacePath;
    private Guid _workspaceId;

    public KickOffPlanEndpointTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        factory.InitializeDatabase();
        _client = factory.CreateClient();
        _workspacePath = Path.Combine(Path.GetTempPath(), $"orchi-kickoff-{Guid.NewGuid():N}");
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
    public async Task KickOffPlan_CreatesPlanFileAndChildChat()
    {
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(_workspaceId, "cursor", OrchestrationAgentModeStrategy.Mode));

        CreateChatResponse? parent = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(parent);

        HttpResponseMessage kickoffResponse = await _client.PostAsJsonAsync(
            $"/chats/{parent.Id}/plans/kickoff",
            new KickOffPlanRequest(
                "auth-refactor",
                "Auth refactor",
                "# Auth refactor\n\nImplement JWT auth."));

        Assert.Equal(HttpStatusCode.Created, kickoffResponse.StatusCode);

        KickOffPlanResponse? kickedOff = await kickoffResponse.Content.ReadFromJsonAsync<KickOffPlanResponse>();
        Assert.NotNull(kickedOff);
        Assert.Equal(".orchi/plan-auth-refactor.md", kickedOff.PlanFilePath);
        Assert.Contains(".orchi/plan-auth-refactor.md", kickedOff.InitialPrompt);
        Assert.Contains("delete `.orchi/plan-auth-refactor.md`", kickedOff.InitialPrompt);
        Assert.Equal("Begin implementation.", kickedOff.KickoffMessage);

        // Same-workspace kickoff keeps the plan file for the implementation child.
        string planFile = Path.Combine(_workspacePath, ".orchi", "plan-auth-refactor.md");
        Assert.True(File.Exists(planFile));

        HttpResponseMessage childResponse = await _client.GetAsync($"/chats/{kickedOff.ChildChatId}");
        Assert.Equal(HttpStatusCode.OK, childResponse.StatusCode);

        ChatDetailResponse? child = await childResponse.Content.ReadFromJsonAsync<ChatDetailResponse>(
            HttpResponseExtensions.JsonOptions);
        Assert.NotNull(child);
        Assert.Equal(ImplementationAgentModeStrategy.Mode, child.Mode);
        Assert.Equal(parent.Id, child.ParentChatId);
        Assert.Equal(".orchi/plan-auth-refactor.md", child.PlanFilePath);
        Assert.Equal(parent.WorkspaceId, child.WorkspaceId);
        Assert.Equal(parent.ProjectId, child.ProjectId);

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            IDbContextFactory<AppDbContext> factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using AppDbContext db = await factory.CreateDbContextAsync();
            Plan? plan = await db.Plans
                .FirstOrDefaultAsync(row => row.PlanId == "auth-refactor" && row.SourceChatId == parent.Id);

            Assert.NotNull(plan);
            Assert.Equal("Auth refactor", plan.Title);
            Assert.Contains("Implement JWT auth", plan.ContentMarkdown);
        }
    }

    [Fact]
    public async Task KickOffPlan_WithWorktree_DeletesPrimaryPlanFileAfterInjectingIntoChild()
    {
        if (!IsGitAvailable())
        {
            return;
        }

        string repoPath = Path.Combine(Path.GetTempPath(), $"orchi-kickoff-worktree-{Guid.NewGuid():N}");
        Directory.CreateDirectory(repoPath);

        try
        {
            InitializeGitRepo(repoPath);

            Directory.CreateDirectory(Path.Combine(repoPath, ".orchi"));
            string primaryPlanPath = Path.Combine(repoPath, ".orchi", "plan-auth-refactor.md");
            await File.WriteAllTextAsync(primaryPlanPath, "# Auth refactor\n\nImplement JWT auth.\n");

            HttpResponseMessage projectResponse = await _client.PostAsJsonAsync(
                "/projects",
                new CreateProjectRequest("Worktree Kickoff Project", repoPath));

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

            KickOffPlanResponse? kickedOff = await kickoffResponse.Content.ReadFromJsonAsync<KickOffPlanResponse>();
            Assert.NotNull(kickedOff);

            HttpResponseMessage childResponse = await _client.GetAsync($"/chats/{kickedOff.ChildChatId}");
            ChatDetailResponse? child = await childResponse.Content.ReadFromJsonAsync<ChatDetailResponse>(
                HttpResponseExtensions.JsonOptions);
            Assert.NotNull(child);
            Assert.NotEqual(parent.WorkspaceId, child.WorkspaceId);
            Assert.NotEqual(parent.WorkspacePath, child.WorkspacePath);

            Assert.False(File.Exists(primaryPlanPath));

            string childPlanPath = Path.Combine(
                child.WorkspacePath,
                kickedOff.PlanFilePath.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(childPlanPath));
            Assert.Contains("Implement JWT auth", await File.ReadAllTextAsync(childPlanPath));
        }
        finally
        {
            if (Directory.Exists(repoPath))
            {
                try
                {
                    Directory.Delete(repoPath, recursive: true);
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
    public async Task KickOffPlan_OnDefaultChat_ReturnsValidationError()
    {
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/chats",
            new CreateChatRequest(_workspaceId, "cursor"));

        CreateChatResponse? parent = await createResponse.Content.ReadFromJsonAsync<CreateChatResponse>();
        Assert.NotNull(parent);

        HttpResponseMessage kickoffResponse = await _client.PostAsJsonAsync(
            $"/chats/{parent.Id}/plans/kickoff",
            new KickOffPlanRequest("auth-refactor", "Auth refactor", "# Plan"));

        Assert.Equal(HttpStatusCode.BadRequest, kickoffResponse.StatusCode);
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
