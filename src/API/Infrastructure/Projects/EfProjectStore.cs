using Microsoft.EntityFrameworkCore;
using Orchi.Api.Common;
using Orchi.Api.Data;
using Orchi.Api.Entities;

namespace Orchi.Api.Infrastructure.Projects;

public sealed class EfProjectStore(IDbContextFactory<AppDbContext> dbContextFactory) : IProjectStore
{
    public async Task<Project?> GetProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Projects
            .AsNoTracking()
            .Include(project => project.Workspaces)
            .FirstOrDefaultAsync(project => project.Id == projectId, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> ListProjectsAsync(CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        List<Project> projects = await db.Projects
            .AsNoTracking()
            .Include(project => project.Workspaces)
            .ToListAsync(cancellationToken);

        return projects
            .OrderByDescending(project => project.UpdatedAt)
            .ToList();
    }

    public async Task<Workspace?> GetWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(workspace => workspace.Id == workspaceId, cancellationToken);
    }

    public async Task<ProjectCreateResult> CreateProjectAsync(
        string name,
        string defaultWorkspacePath,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string fullPath = Path.GetFullPath(defaultWorkspacePath);
        string normalizedPath = WorkspacePathNormalizer.Normalize(fullPath);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Path = fullPath,
            NormalizedPath = normalizedPath,
            Name = WorkspacePathNormalizer.DeriveNameFromPath(fullPath),
            IsDefault = true,
            Kind = WorkspaceKind.Primary,
            CreatedAt = now
        };

        db.Projects.Add(project);
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);

        project.Workspaces.Add(workspace);
        return new ProjectCreateResult(project, workspace);
    }

    public async Task<Project?> UpdateProjectAsync(
        Guid projectId,
        string name,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Project? project = await db.Projects
            .Include(existing => existing.Workspaces)
            .FirstOrDefaultAsync(existing => existing.Id == projectId, cancellationToken);

        if (project is null)
        {
            return null;
        }

        project.Name = name.Trim();
        project.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task<ProjectDeleteResult?> DeleteProjectAsync(Guid projectId, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Project? project = await db.Projects.FirstOrDefaultAsync(existing => existing.Id == projectId, cancellationToken);
        if (project is null)
        {
            return null;
        }

        List<Guid> workspaceIds = await db.Workspaces
            .Where(workspace => workspace.ProjectId == projectId)
            .Select(workspace => workspace.Id)
            .ToListAsync(cancellationToken);

        List<Chat> orphanChats = await db.Chats
            .IgnoreQueryFilters()
            .Where(chat => chat.ProjectId == projectId || (chat.WorkspaceId != null && workspaceIds.Contains(chat.WorkspaceId.Value)))
            .ToListAsync(cancellationToken);

        foreach (Chat chat in orphanChats)
        {
            chat.ProjectId = null;
            chat.WorkspaceId = null;
        }

        db.Projects.Remove(project);
        await db.SaveChangesAsync(cancellationToken);
        return new ProjectDeleteResult(orphanChats.Select(chat => chat.Id).ToList());
    }

    public async Task<WorkspaceCreateResult?> CreateWorkspaceAsync(
        Guid projectId,
        string path,
        string? name,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        bool projectExists = await db.Projects.AnyAsync(existing => existing.Id == projectId, cancellationToken);
        if (!projectExists)
        {
            return null;
        }

        string fullPath = Path.GetFullPath(path);
        string normalizedPath = WorkspacePathNormalizer.Normalize(fullPath);
        bool duplicatePath = await db.Workspaces.AnyAsync(
            workspace => workspace.ProjectId == projectId && workspace.NormalizedPath == normalizedPath,
            cancellationToken);

        if (duplicatePath)
        {
            throw new InvalidOperationException($"A workspace with path '{fullPath}' already exists in this project.");
        }

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Path = fullPath,
            NormalizedPath = normalizedPath,
            Name = string.IsNullOrWhiteSpace(name)
                ? WorkspacePathNormalizer.DeriveNameFromPath(fullPath)
                : name.Trim(),
            IsDefault = false,
            Kind = WorkspaceKind.Primary,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(cancellationToken);
        return new WorkspaceCreateResult(workspace);
    }

    public async Task<Workspace?> UpdateWorkspaceAsync(
        Guid workspaceId,
        string? name,
        bool? isDefault,
        CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Workspace? workspace = await db.Workspaces
            .FirstOrDefaultAsync(existing => existing.Id == workspaceId, cancellationToken);

        if (workspace is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            workspace.Name = name.Trim();
        }

        if (isDefault == true)
        {
            List<Workspace> siblings = await db.Workspaces
                .Where(existing => existing.ProjectId == workspace.ProjectId)
                .ToListAsync(cancellationToken);

            foreach (Workspace sibling in siblings)
            {
                sibling.IsDefault = sibling.Id == workspaceId;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return workspace;
    }

    public async Task<bool> DeleteWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        await using AppDbContext db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        Workspace? workspace = await db.Workspaces
            .FirstOrDefaultAsync(existing => existing.Id == workspaceId, cancellationToken);

        if (workspace is null)
        {
            return false;
        }

        int workspaceCount = await db.Workspaces.CountAsync(
            existing => existing.ProjectId == workspace.ProjectId,
            cancellationToken);

        if (workspaceCount <= 1)
        {
            throw new InvalidOperationException("Cannot delete the last workspace in a project.");
        }

        List<Chat> orphanChats = await db.Chats
            .IgnoreQueryFilters()
            .Where(chat => chat.WorkspaceId == workspaceId)
            .ToListAsync(cancellationToken);

        foreach (Chat chat in orphanChats)
        {
            chat.WorkspaceId = null;
        }

        db.Workspaces.Remove(workspace);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
