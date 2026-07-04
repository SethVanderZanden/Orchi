using Orchi.Api.Entities;

namespace Orchi.Api.Features.Projects.Shared;

public static class ProjectMapper
{
    public static WorkspaceResponse ToWorkspaceResponse(Workspace workspace) =>
        new(
            workspace.Id,
            workspace.ProjectId,
            workspace.Path,
            workspace.Name,
            workspace.IsDefault,
            workspace.Kind.ToString(),
            workspace.CreatedAt);

    public static ProjectSummaryResponse ToSummary(Project project) =>
        new(
            project.Id,
            project.Name,
            project.CreatedAt,
            project.UpdatedAt,
            project.Workspaces.Select(ToWorkspaceResponse).ToArray());

    public static ProjectDetailResponse ToDetail(Project project) =>
        new(
            project.Id,
            project.Name,
            project.CreatedAt,
            project.UpdatedAt,
            project.Workspaces.Select(ToWorkspaceResponse).ToArray());

    public static CreateProjectResponse ToCreateResponse(Project project, Workspace defaultWorkspace) =>
        new(
            project.Id,
            project.Name,
            project.CreatedAt,
            project.UpdatedAt,
            ToWorkspaceResponse(defaultWorkspace));
}

public sealed record CreateProjectRequest(string Name, string DefaultWorkspacePath);

public sealed record CreateProjectResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    WorkspaceResponse DefaultWorkspace);

public sealed record ProjectSummaryResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<WorkspaceResponse> Workspaces);

public sealed record ProjectDetailResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<WorkspaceResponse> Workspaces);

public sealed record UpdateProjectRequest(string Name);

public sealed record CreateWorkspaceRequest(string Path, string? Name = null);

public sealed record UpdateWorkspaceRequest(string? Name = null, bool? IsDefault = null);

public sealed record WorkspaceResponse(
    Guid Id,
    Guid ProjectId,
    string Path,
    string Name,
    bool IsDefault,
    string Kind,
    DateTimeOffset CreatedAt);
