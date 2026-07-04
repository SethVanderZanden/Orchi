import { getApiBaseUrl } from '@/lib/api'

import type {
  CreateProjectRequest,
  CreateProjectResponse,
  CreateWorkspaceRequest,
  ProjectDetailResponse,
  ProjectSummaryResponse,
  UpdateProjectRequest,
  UpdateWorkspaceRequest,
  WorkspaceResponse
} from './types'

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const body = (await response.json()) as {
      message?: string
      Message?: string
      title?: string
      detail?: string
    }

    if (body.detail) {
      return body.detail
    }

    return body.message ?? body.Message ?? `API error: ${response.status}`
  } catch {
    return `API error: ${response.status}`
  }
}

function mapWorkspace(workspace: WorkspaceResponse) {
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

function mapProjectSummary(summary: ProjectSummaryResponse) {
  return {
    id: summary.id,
    name: summary.name,
    createdAt: summary.createdAt,
    updatedAt: summary.updatedAt,
    workspaces: summary.workspaces.map(mapWorkspace)
  }
}

function mapProjectDetail(detail: ProjectDetailResponse) {
  return {
    id: detail.id,
    name: detail.name,
    createdAt: detail.createdAt,
    updatedAt: detail.updatedAt,
    workspaces: detail.workspaces.map(mapWorkspace)
  }
}

export async function listProjects() {
  const response = await fetch(`${getApiBaseUrl()}/projects`)
  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const summaries = (await response.json()) as ProjectSummaryResponse[]
  return summaries.map(mapProjectSummary)
}

export async function getProject(projectId: string) {
  const response = await fetch(`${getApiBaseUrl()}/projects/${projectId}`)
  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  const detail = (await response.json()) as ProjectDetailResponse
  return mapProjectDetail(detail)
}

export async function createProject(request: CreateProjectRequest) {
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

export async function updateProject(projectId: string, request: UpdateProjectRequest) {
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

export async function deleteProject(projectId: string) {
  const response = await fetch(`${getApiBaseUrl()}/projects/${projectId}`, {
    method: 'DELETE'
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }
}

export async function createWorkspace(projectId: string, request: CreateWorkspaceRequest) {
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

export async function updateWorkspace(workspaceId: string, request: UpdateWorkspaceRequest) {
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

export async function deleteWorkspace(workspaceId: string) {
  const response = await fetch(`${getApiBaseUrl()}/workspaces/${workspaceId}`, {
    method: 'DELETE'
  })

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }
}
