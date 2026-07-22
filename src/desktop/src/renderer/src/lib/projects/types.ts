export type WorkspaceKind = 'primary' | 'worktree'

export type GitHostProvider = 'github' | 'azureDevOps'

export type Workspace = {
  id: string
  projectId: string
  path: string
  name: string
  isDefault: boolean
  kind: WorkspaceKind
  branch: string | null
  baseBranch: string | null
  createdAt: string
}

export type Project = {
  id: string
  name: string
  defaultBaseBranch: string
  defaultWorktreeBranchPattern: string
  gitHostProvider: GitHostProvider
  useWorktreeOnKickoff: boolean
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
  defaultBaseBranch: string
  defaultWorktreeBranchPattern: string
  gitHostProvider: string
  useWorktreeOnKickoff: boolean
  createdAt: string
  updatedAt: string
  defaultWorkspace: WorkspaceResponse
}

export type ProjectSummaryResponse = {
  id: string
  name: string
  defaultBaseBranch: string
  defaultWorktreeBranchPattern: string
  gitHostProvider: string
  useWorktreeOnKickoff: boolean
  createdAt: string
  updatedAt: string
  workspaces: WorkspaceResponse[]
}

export type ProjectDetailResponse = {
  id: string
  name: string
  defaultBaseBranch: string
  defaultWorktreeBranchPattern: string
  gitHostProvider: string
  useWorktreeOnKickoff: boolean
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
  branch?: string | null
  baseBranch?: string | null
  createdAt: string
}

export type UpdateProjectRequest = {
  name?: string
  defaultBaseBranch?: string
  defaultWorktreeBranchPattern?: string
  gitHostProvider?: GitHostProvider
  useWorktreeOnKickoff?: boolean
}

export type CreateWorkspaceRequest = {
  path: string
  name?: string
  kind?: string
  branch?: string
  baseBranch?: string
}

export type UpdateWorkspaceRequest = {
  name?: string
  isDefault?: boolean
}

export type ProjectBranch = {
  name: string
  isCurrent: boolean
  isRemote?: boolean
}
