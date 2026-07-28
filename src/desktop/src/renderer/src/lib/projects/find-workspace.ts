import type { Project, Workspace } from '@/lib/projects/types'

export function findWorkspaceInProjects(
  projects: Project[],
  workspaceId: string | null
): Workspace | undefined {
  if (!workspaceId) {
    return undefined
  }

  for (const project of projects) {
    const workspace = project.workspaces.find((entry) => entry.id === workspaceId)
    if (workspace) {
      return workspace
    }
  }

  return undefined
}

export function findProjectForWorkspace(
  projects: Project[],
  workspaceId: string | null
): Project | undefined {
  if (!workspaceId) {
    return undefined
  }

  return projects.find((project) => project.workspaces.some((workspace) => workspace.id === workspaceId))
}

export function resolveWorkspaceContext(
  projects: Project[],
  projectId: string | null,
  workspaceId: string | null,
  workspaceName: string | null,
  projectName: string | null
): {
  project: Project | undefined
  workspace: Workspace | undefined
  resolvedProjectName: string | null
  resolvedWorkspaceName: string
} {
  const project =
    projects.find((entry) => entry.id === projectId) ?? findProjectForWorkspace(projects, workspaceId)
  const workspace =
    project?.workspaces.find((entry) => entry.id === workspaceId) ??
    findWorkspaceInProjects(projects, workspaceId)

  return {
    project,
    workspace,
    resolvedProjectName: projectName ?? project?.name ?? (projectId ? 'Unknown project' : null),
    resolvedWorkspaceName: workspaceName ?? workspace?.name ?? 'No workspace'
  }
}
