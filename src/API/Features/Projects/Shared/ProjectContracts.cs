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
            ToKindName(workspace.Kind),
            workspace.Branch,
            workspace.BaseBranch,
            workspace.CreatedAt);

    public static ProjectSummaryResponse ToSummary(Project project) =>
        new(
            project.Id,
            project.Name,
            project.DefaultBaseBranch,
            project.DefaultWorktreeBranchPattern,
            ToGitHostName(project.GitHostProvider),
            project.UseWorktreeOnKickoff,
            project.CreatedAt,
            project.UpdatedAt,
            project.Workspaces.Select(ToWorkspaceResponse).ToArray());

    public static ProjectDetailResponse ToDetail(Project project) =>
        new(
            project.Id,
            project.Name,
            project.DefaultBaseBranch,
            project.DefaultWorktreeBranchPattern,
            ToGitHostName(project.GitHostProvider),
            project.UseWorktreeOnKickoff,
            project.CreatedAt,
            project.UpdatedAt,
            project.Workspaces.Select(ToWorkspaceResponse).ToArray());

    public static CreateProjectResponse ToCreateResponse(Project project, Workspace defaultWorkspace) =>
        new(
            project.Id,
            project.Name,
            project.DefaultBaseBranch,
            project.DefaultWorktreeBranchPattern,
            ToGitHostName(project.GitHostProvider),
            project.UseWorktreeOnKickoff,
            project.CreatedAt,
            project.UpdatedAt,
            ToWorkspaceResponse(defaultWorkspace));

    public static string ToGitHostName(GitHostProvider provider) =>
        provider switch
        {
            GitHostProvider.GitHub => "github",
            GitHostProvider.AzureDevOps => "azureDevOps",
            _ => provider.ToString()
        };

    public static bool TryParseGitHost(string? value, out GitHostProvider provider)
    {
        provider = GitHostProvider.GitHub;
        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(value, "github", StringComparison.OrdinalIgnoreCase))
        {
            provider = GitHostProvider.GitHub;
            return true;
        }

        if (string.Equals(value, "azureDevOps", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "AzureDevOps", StringComparison.OrdinalIgnoreCase))
        {
            provider = GitHostProvider.AzureDevOps;
            return true;
        }

        return false;
    }

    private static string ToKindName(WorkspaceKind kind) =>
        kind switch
        {
            WorkspaceKind.Primary => "primary",
            WorkspaceKind.Worktree => "worktree",
            _ => kind.ToString()
        };
}

public sealed record CreateProjectRequest(string Name, string DefaultWorkspacePath);

public sealed record CreateProjectResponse(
    Guid Id,
    string Name,
    string DefaultBaseBranch,
    string DefaultWorktreeBranchPattern,
    string GitHostProvider,
    bool UseWorktreeOnKickoff,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    WorkspaceResponse DefaultWorkspace);

public sealed record ProjectSummaryResponse(
    Guid Id,
    string Name,
    string DefaultBaseBranch,
    string DefaultWorktreeBranchPattern,
    string GitHostProvider,
    bool UseWorktreeOnKickoff,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<WorkspaceResponse> Workspaces);

public sealed record ProjectDetailResponse(
    Guid Id,
    string Name,
    string DefaultBaseBranch,
    string DefaultWorktreeBranchPattern,
    string GitHostProvider,
    bool UseWorktreeOnKickoff,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<WorkspaceResponse> Workspaces);

public sealed record UpdateProjectRequest(
    string? Name = null,
    string? DefaultBaseBranch = null,
    string? DefaultWorktreeBranchPattern = null,
    string? GitHostProvider = null,
    bool? UseWorktreeOnKickoff = null);

public sealed record CreateWorkspaceRequest(
    string Path,
    string? Name = null,
    string? Kind = null,
    string? Branch = null,
    string? BaseBranch = null);

public sealed record UpdateWorkspaceRequest(string? Name = null, bool? IsDefault = null);

public sealed record WorkspaceResponse(
    Guid Id,
    Guid ProjectId,
    string Path,
    string Name,
    bool IsDefault,
    string Kind,
    string? Branch,
    string? BaseBranch,
    DateTimeOffset CreatedAt);

public sealed record ProjectBranchResponse(string Name, bool IsCurrent);
