export type WorkspaceKind = 'primary' | 'worktree'

export type Workspace = {
  id: string
  projectId: string
  path: string
  name: string
  isDefault: boolean
  kind: WorkspaceKind
  createdAt: string
}

export type Project = {
  id: string
  name: string
  workspaces: Workspace[]
  createdAt: string
  updatedAt: string
}

export type CreateProjectRequest = {
  name: string
  defaultWorkspacePath: string
}

export type CreateProjectResponse = {
  id: string
  name: string
  createdAt: string
  updatedAt: string
  defaultWorkspace: WorkspaceResponse
}

export type ProjectSummaryResponse = {
  id: string
  name: string
  createdAt: string
  updatedAt: string
  workspaces: WorkspaceResponse[]
}

export type ProjectDetailResponse = {
  id: string
  name: string
  createdAt: string
  updatedAt: string
  workspaces: WorkspaceResponse[]
}

export type WorkspaceResponse = {
  id: string
  projectId: string
  path: string
  name: string
  isDefault: boolean
  kind: string
  createdAt: string
}

export type UpdateProjectRequest = {
  name: string
}

export type CreateWorkspaceRequest = {
  path: string
  name?: string
}

export type UpdateWorkspaceRequest = {
  name?: string
  isDefault?: boolean
}
