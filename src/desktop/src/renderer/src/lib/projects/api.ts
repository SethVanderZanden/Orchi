import { getApiBaseUrl } from '@/lib/api'
import { readErrorMessage } from '@/lib/http/read-error-message'

import type {
  CreateProjectRequest,
  CreateProjectResponse,
  CreateWorkspaceRequest,
  Project,
  ProjectDetailResponse,
  ProjectSummaryResponse,
  UpdateProjectRequest,
  UpdateWorkspaceRequest,
  Workspace,
  WorkspaceResponse
} from './types'

function mapWorkspace(workspace: WorkspaceResponse): Workspace {
  return {
    id: workspace.id,
    projectId: workspace.projectId,
    path: workspace.path,
    name: workspace.name,
    isDefault: workspace.isDefault,
    kind: (workspace.kind.toLowerCase() === 'worktree' ? 'worktree' : 'primary') as
      | 'primary'
      | 'worktree',
    createdAt: workspace.createdAt
  }
}

function mapProjectSummary(summary: ProjectSummaryResponse): Project {
  return {
    id: summary.id,
    name: summary.name,
    createdAt: summary.createdAt,
    updatedAt: summary.updatedAt,
    workspaces: summary.workspaces.map(mapWorkspace)
  }
}

function mapProjectDetail(detail: ProjectDetailResponse): Project {
  return {
    id: detail.id,
    name: detail.name,
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
