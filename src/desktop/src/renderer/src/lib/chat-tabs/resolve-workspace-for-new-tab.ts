import { getDefaultWorkspace } from '@/lib/projects/group-chats'
import type { Project } from '@/lib/projects/types'
import type { ChatThread } from '@/lib/chat/types'

export type ResolvedNewChatWorkspace = {
  workspaceId: string
  workspacePath: string
  projectId: string | null
}

export type NewChatTabPlan =
  | { kind: 'create'; workspace: ResolvedNewChatWorkspace }
  | { kind: 'needsProject' }

/**
 * Prefer the active tab's workspace; otherwise the first project's default workspace.
 */
export function resolveWorkspaceForNewTab(
  activeChat: ChatThread | undefined,
  projects: Project[]
): ResolvedNewChatWorkspace | null {
  if (activeChat?.workspaceId) {
    return {
      workspaceId: activeChat.workspaceId,
      workspacePath: activeChat.workspacePath,
      projectId: activeChat.projectId
    }
  }

  const firstProject = projects[0]
  if (!firstProject) {
    return null
  }

  const workspace = getDefaultWorkspace(firstProject)
  if (!workspace) {
    return null
  }

  return {
    workspaceId: workspace.id,
    workspacePath: workspace.path,
    projectId: firstProject.id
  }
}

/**
 * Decides whether New Chat can open immediately or must register a project first.
 * Callers must not silently no-op on `needsProject` — route to onboarding / empty state.
 */
export function planNewChatTab(
  activeChat: ChatThread | undefined,
  projects: Project[]
): NewChatTabPlan {
  const workspace = resolveWorkspaceForNewTab(activeChat, projects)
  if (!workspace) {
    return { kind: 'needsProject' }
  }

  return { kind: 'create', workspace }
}
