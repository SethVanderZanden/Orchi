import type { ResolvedNewChatWorkspace } from '@/lib/chat-tabs/resolve-workspace-for-new-tab'
import type { CreateLocalDraftOptions } from '@/lib/chat/create-local-draft'
import type { ChatThread } from '@/lib/chat/types'
import { getDefaultWorkspace } from '@/lib/projects/group-chats'
import type { Project, Workspace } from '@/lib/projects/types'

export type ResolvedChatFromSource = {
  workspace: ResolvedNewChatWorkspace
  draftOptions: Pick<
    CreateLocalDraftOptions,
    | 'mode'
    | 'agentId'
    | 'modelId'
    | 'contextSizeId'
    | 'reasoningEffortId'
    | 'approvalPolicyId'
  >
  enableWorktree: boolean
}

function findProject(projects: Project[], projectId: string | null): Project | undefined {
  if (!projectId) {
    return undefined
  }

  return projects.find((project) => project.id === projectId)
}

function findWorkspace(project: Project, workspaceId: string | null): Workspace | undefined {
  if (!workspaceId) {
    return undefined
  }

  return project.workspaces.find((workspace) => workspace.id === workspaceId)
}

/**
 * Prefer the project's primary workspace so a new worktree can be provisioned on first send.
 * Falls back to the source chat workspace when no project default exists.
 */
export function resolveWorkspaceForChatFromSource(
  sourceChat: ChatThread,
  projects: Project[]
): ResolvedNewChatWorkspace | null {
  const project = findProject(projects, sourceChat.projectId)
  if (project) {
    const workspace = getDefaultWorkspace(project)
    if (workspace) {
      return {
        workspaceId: workspace.id,
        workspacePath: workspace.path,
        projectId: project.id
      }
    }
  }

  if (sourceChat.workspaceId && sourceChat.workspacePath) {
    return {
      workspaceId: sourceChat.workspaceId,
      workspacePath: sourceChat.workspacePath,
      projectId: sourceChat.projectId
    }
  }

  return null
}

export function isSourceChatOnWorktree(sourceChat: ChatThread, projects: Project[]): boolean {
  const project = findProject(projects, sourceChat.projectId)
  if (!project || !sourceChat.workspaceId) {
    return false
  }

  return findWorkspace(project, sourceChat.workspaceId)?.kind === 'worktree'
}

export function resolveChatFromSource(
  sourceChat: ChatThread,
  projects: Project[]
): ResolvedChatFromSource | null {
  const workspace = resolveWorkspaceForChatFromSource(sourceChat, projects)
  if (!workspace) {
    return null
  }

  return {
    workspace,
    draftOptions: {
      mode: sourceChat.mode,
      agentId: sourceChat.agentId,
      modelId: sourceChat.modelId,
      contextSizeId: sourceChat.contextSizeId,
      reasoningEffortId: sourceChat.reasoningEffortId,
      approvalPolicyId: sourceChat.approvalPolicyId
    },
    enableWorktree: workspace.projectId !== null
  }
}
