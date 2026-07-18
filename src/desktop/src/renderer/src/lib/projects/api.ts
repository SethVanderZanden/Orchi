import { getApiBaseUrl } from '@/lib/api'
import { readErrorMessage } from '@/lib/http/read-error-message'

import type {
  CreateProjectRequest,
  CreateProjectResponse,
  CreateWorkspaceRequest,
  GitHostProvider,
  Project,
  ProjectBranch,
  ProjectDetailResponse,
  ProjectSummaryResponse,
  UpdateProjectRequest,
  UpdateWorkspaceRequest,
  Workspace,
  WorkspaceResponse
} from './types'

function mapGitHost(value: string): GitHostProvider {
  return value.toLowerCase() === 'azuredevops' ? 'azureDevOps' : 'github'
}

function mapWorkspace(workspace: WorkspaceResponse): Workspace {
  return {
    id: workspace.id,
    projectId: workspace.projectId,
    path: workspace.path,
    name: workspace.name,
    isDefault: workspace.isDefault,
    kind: workspace.kind.toLowerCase() === 'worktree' ? 'worktree' : 'primary',
    branch: workspace.branch ?? null,
    baseBranch: workspace.baseBranch ?? null,
    createdAt: workspace.createdAt
  }
}

const defaultWorktreeBranchPattern = 'orchi/{date}-{shortId}'

function mapProjectSummary(summary: ProjectSummaryResponse): Project {
  return {
    id: summary.id,
    name: summary.name,
    defaultBaseBranch: summary.defaultBaseBranch ?? 'main',
    defaultWorktreeBranchPattern:
      summary.defaultWorktreeBranchPattern ?? defaultWorktreeBranchPattern,
    gitHostProvider: mapGitHost(summary.gitHostProvider ?? 'github'),
    useWorktreeOnKickoff: summary.useWorktreeOnKickoff ?? true,
    createdAt: summary.createdAt,
    updatedAt: summary.updatedAt,
    workspaces: summary.workspaces.map(mapWorkspace)
  }
}

function mapProjectDetail(detail: ProjectDetailResponse): Project {
  return {
    id: detail.id,
    name: detail.name,
    defaultBaseBranch: detail.defaultBaseBranch ?? 'main',
    defaultWorktreeBranchPattern:
      detail.defaultWorktreeBranchPattern ?? defaultWorktreeBranchPattern,
    gitHostProvider: mapGitHost(detail.gitHostProvider ?? 'github'),
    useWorktreeOnKickoff: detail.useWorktreeOnKickoff ?? true,
    createdAt: detail.createdAt,
    updatedAt: detail.updatedAt,
    workspaces: detail.workspaces.map(mapWorkspace)
  }
}

export async function listProjects(): Promise<Project[]> {
  const response = await fetch(`${getApiBaseUrl()}/projects`)
  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const summaries = (await response.json()) as ProjectSummaryResponse[]
  return summaries.map(mapProjectSummary)
}

export async function getProject(projectId: string): Promise<Project> {
  const response = await fetch(`${getApiBaseUrl()}/projects/${projectId}`)
  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const detail = (await response.json()) as ProjectDetailResponse
  return mapProjectDetail(detail)
}

export async function createProject(request: CreateProjectRequest): Promise<Project> {
  const response = await fetch(`${getApiBaseUrl()}/projects`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const created = (await response.json()) as CreateProjectResponse
  return {
    id: created.id,
    name: created.name,
    defaultBaseBranch: created.defaultBaseBranch ?? 'main',
    defaultWorktreeBranchPattern:
      created.defaultWorktreeBranchPattern ?? defaultWorktreeBranchPattern,
    gitHostProvider: mapGitHost(created.gitHostProvider ?? 'github'),
    useWorktreeOnKickoff: created.useWorktreeOnKickoff ?? true,
    createdAt: created.createdAt,
    updatedAt: created.updatedAt,
    workspaces: [mapWorkspace(created.defaultWorkspace)]
  }
}

export async function updateProject(
  projectId: string,
  request: UpdateProjectRequest
): Promise<Project> {
  const response = await fetch(`${getApiBaseUrl()}/projects/${projectId}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const updated = (await response.json()) as ProjectDetailResponse
  return mapProjectDetail(updated)
}

export async function deleteProject(projectId: string): Promise<void> {
  const response = await fetch(`${getApiBaseUrl()}/projects/${projectId}`, {
    method: 'DELETE'
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }
}

export async function createWorkspace(
  projectId: string,
  request: CreateWorkspaceRequest
): Promise<Workspace> {
  const response = await fetch(`${getApiBaseUrl()}/projects/${projectId}/workspaces`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const created = (await response.json()) as WorkspaceResponse
  return mapWorkspace(created)
}

export async function updateWorkspace(
  workspaceId: string,
  request: UpdateWorkspaceRequest
): Promise<Workspace> {
  const response = await fetch(`${getApiBaseUrl()}/workspaces/${workspaceId}`, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const updated = (await response.json()) as WorkspaceResponse
  return mapWorkspace(updated)
}

export async function deleteWorkspace(workspaceId: string): Promise<void> {
  const response = await fetch(`${getApiBaseUrl()}/workspaces/${workspaceId}`, {
    method: 'DELETE'
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }
}

export async function listProjectBranches(projectId: string): Promise<ProjectBranch[]> {
  const response = await fetch(`${getApiBaseUrl()}/projects/${projectId}/branches`)
  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as ProjectBranch[]
}

export type CreateWorktreeRequest = {
  baseBranch?: string | null
  branchName?: string | null
  name?: string | null
}

export async function createWorktree(
  projectId: string,
  request: CreateWorktreeRequest = {}
): Promise<Workspace> {
  const response = await fetch(`${getApiBaseUrl()}/projects/${projectId}/worktrees`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request)
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const created = (await response.json()) as WorkspaceResponse
  return mapWorkspace(created)
}
