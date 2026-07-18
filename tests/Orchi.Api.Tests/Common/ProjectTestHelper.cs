using System.Net.Http.Json;
using Orchi.Api.Features.Projects.Shared;

namespace Orchi.Api.Tests.Common;

public static class ProjectTestHelper
{
    public static async Task<Guid> CreateProjectWithWorkspaceAsync(
        HttpClient client,
        string workspacePath,
        string? projectName = null)
    {
        string name = projectName ?? Path.GetFileName(workspacePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "Project";
        }

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/projects",
            new CreateProjectRequest(name, workspacePath));

        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to create project ({response.StatusCode}): {body}");
        }

        CreateProjectResponse? created = await response.Content.ReadFromJsonAsync<CreateProjectResponse>();
        if (created is null)
        {
            throw new InvalidOperationException("Failed to create project for test setup.");
        }

        // Integration tests share non-worktree folders; disable automatic worktree kickoff.
        HttpResponseMessage patch = await client.PatchAsJsonAsync(
            $"/projects/{created.Id}",
            new UpdateProjectRequest(UseWorktreeOnKickoff: false));

        if (!patch.IsSuccessStatusCode)
        {
            string body = await patch.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to disable worktree kickoff ({patch.StatusCode}): {body}");
        }

        return created.DefaultWorkspace.Id;
    }
}
