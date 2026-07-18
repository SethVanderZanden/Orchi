using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Projects;

public sealed record ProjectCreateResult(Project Project, Workspace DefaultWorkspace);

public sealed record WorkspaceCreateResult(Workspace Workspace);

public sealed record ProjectDeleteResult(IReadOnlyList<Guid> OrphanedChatIds);

public interface IProjectStore
{
    Task<Project?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Project>> ListProjectsAsync(CancellationToken cancellationToken);

    Task<Workspace?> GetWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken);

    Task<ProjectCreateResult> CreateProjectAsync(
        string name,
        string defaultWorkspacePath,
        CancellationToken cancellationToken);

    Task<Project?> UpdateProjectAsync(
        Guid projectId,
        string? name,
        string? defaultBaseBranch,
        string? defaultWorktreeBranchPattern,
        GitHostProvider? gitHostProvider,
        bool? useWorktreeOnKickoff,
        CancellationToken cancellationToken);

    Task<ProjectDeleteResult?> DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken);

    Task<WorkspaceCreateResult?> CreateWorkspaceAsync(
        Guid projectId,
        string path,
        string? name,
        WorkspaceKind kind,
        string? branch,
        string? baseBranch,
        CancellationToken cancellationToken);

    Task<Workspace?> UpdateWorkspaceAsync(
        Guid workspaceId,
        string? name,
        bool? isDefault,
        CancellationToken cancellationToken);

    Task<bool> DeleteWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken);
}
