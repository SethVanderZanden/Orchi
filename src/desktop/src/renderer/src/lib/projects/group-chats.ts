import type { ChatThread } from '@/lib/chat/types'
import { buildChatTree, filterChatTreeNodes, type ChatTreeNode } from '@/lib/workspaces/chat-tree'
import { normalizeWorkspacePath } from './paths'
import type { Project, Workspace } from './types'

export type WorkspaceChatSubGroup = {
  id: string
  name: string
  path: string
  kind: Workspace['kind']
  isDefault: boolean
  chatNodes: ChatTreeNode[]
}

export type ProjectChatGroup = {
  id: string
  name: string
  isOrphan: boolean
  /** When true, chats render directly under the project (single workspace). */
  isFlat: boolean
  defaultWorkspaceId: string | null
  defaultWorkspacePath: string
  chatNodes: ChatTreeNode[]
  workspaceGroups: WorkspaceChatSubGroup[]
}

export const ORPHAN_GROUP_ID = '__orphan__'

export function getDefaultWorkspace(project: Project): Workspace | undefined {
  return project.workspaces.find((workspace) => workspace.isDefault) ?? project.workspaces[0]
}

function chatMatchesWorkspace(chat: ChatThread, workspace: Workspace): boolean {
  if (chat.workspaceId !== null) {
    return chat.workspaceId === workspace.id
  }

  return normalizeWorkspacePath(chat.workspacePath) === normalizeWorkspacePath(workspace.path)
}

function chatMatchesProject(chat: ChatThread, project: Project): boolean {
  if (chat.projectId !== null) {
    return chat.projectId === project.id
  }

  if (chat.workspaceId !== null) {
    return project.workspaces.some((workspace) => workspace.id === chat.workspaceId)
  }

  return project.workspaces.some((workspace) => chatMatchesWorkspace(chat, workspace))
}

function assignChatsToWorkspace(
  chats: ChatThread[],
  workspace: Workspace,
  matchedChatIds: Set<string>
): ChatThread[] {
  const workspaceChats: ChatThread[] = []

  for (const chat of chats) {
    if (matchedChatIds.has(chat.id)) {
      continue
    }

    if (!chatMatchesWorkspace(chat, workspace)) {
      continue
    }

    matchedChatIds.add(chat.id)
    workspaceChats.push(chat)
  }

  return workspaceChats
}

export function groupChatsByProject(projects: Project[], chats: ChatThread[]): ProjectChatGroup[] {
  const matchedChatIds = new Set<string>()
  const groups: ProjectChatGroup[] = []

  const sortedProjects = [...projects].sort((a, b) => a.name.localeCompare(b.name))

  for (const project of sortedProjects) {
    const defaultWorkspace = getDefaultWorkspace(project)
    const isFlat = project.workspaces.length <= 1

    if (isFlat) {
      const projectChats: ChatThread[] = []

      for (const chat of chats) {
        if (matchedChatIds.has(chat.id)) {
          continue
        }

        if (!chatMatchesProject(chat, project)) {
          continue
        }

        matchedChatIds.add(chat.id)
        projectChats.push(chat)
      }

      groups.push({
        id: project.id,
        name: project.name,
        isOrphan: false,
        isFlat: true,
        defaultWorkspaceId: defaultWorkspace?.id ?? null,
        defaultWorkspacePath: defaultWorkspace?.path ?? '',
        chatNodes: buildChatTree(projectChats),
        workspaceGroups: []
      })

      continue
    }

    const workspaceGroups: WorkspaceChatSubGroup[] = []

    for (const workspace of project.workspaces) {
      const workspaceChats = assignChatsToWorkspace(chats, workspace, matchedChatIds)

      workspaceGroups.push({
        id: workspace.id,
        name: workspace.name,
        path: workspace.path,
        kind: workspace.kind,
        isDefault: workspace.isDefault,
        chatNodes: buildChatTree(workspaceChats)
      })
    }

    groups.push({
      id: project.id,
      name: project.name,
      isOrphan: false,
      isFlat: false,
      defaultWorkspaceId: defaultWorkspace?.id ?? null,
      defaultWorkspacePath: defaultWorkspace?.path ?? '',
      chatNodes: [],
      workspaceGroups: workspaceGroups.sort((a, b) => a.name.localeCompare(b.name))
    })
  }

  const orphanChats = chats.filter((chat) => !matchedChatIds.has(chat.id))

  if (orphanChats.length > 0) {
    groups.push({
      id: ORPHAN_GROUP_ID,
      name: 'Other',
      isOrphan: true,
      isFlat: true,
      defaultWorkspaceId: null,
      defaultWorkspacePath: '',
      chatNodes: buildChatTree(orphanChats),
      workspaceGroups: []
    })
  }

  return groups
}

export function filterProjectGroups(
  groups: ProjectChatGroup[],
  searchQuery: string
): ProjectChatGroup[] {
  const query = searchQuery.trim().toLowerCase()
  if (!query) {
    return groups
  }

  return groups
    .map((group) => {
      if (group.isFlat) {
        return {
          ...group,
          chatNodes: filterChatTreeNodes(group.chatNodes, query)
        }
      }

      const workspaceGroups = group.workspaceGroups
        .map((workspaceGroup) => ({
          ...workspaceGroup,
          chatNodes: filterChatTreeNodes(workspaceGroup.chatNodes, query)
        }))
        .filter((workspaceGroup) => workspaceGroup.chatNodes.length > 0)

      return {
        ...group,
        workspaceGroups
      }
    })
    .filter((group) => {
      if (group.isFlat) {
        return group.chatNodes.length > 0
      }

      return group.workspaceGroups.length > 0
    })
}

export function groupContainsChat(group: ProjectChatGroup, chatId: string): boolean {
  const nodes = group.isFlat
    ? group.chatNodes
    : group.workspaceGroups.flatMap((workspaceGroup) => workspaceGroup.chatNodes)

  return nodes.some(
    (node) => node.chat.id === chatId || node.children.some((child) => child.id === chatId)
  )
}

export function findProjectGroupForChat(
  groups: ProjectChatGroup[],
  chat: ChatThread
): ProjectChatGroup | undefined {
  return groups.find((group) => groupContainsChat(group, chat.id))
}

export function resolveWorkspaceIdForNewChat(
  group: ProjectChatGroup,
  workspaceSubGroupId?: string
): string | null {
  if (group.isOrphan) {
    return null
  }

  if (workspaceSubGroupId) {
    const workspaceGroup = group.workspaceGroups.find(
      (workspace) => workspace.id === workspaceSubGroupId
    )
    return workspaceGroup?.id ?? group.defaultWorkspaceId
  }

  return group.defaultWorkspaceId
}
