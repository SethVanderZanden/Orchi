import type { ChatThread } from '@/lib/chat/types'
import type { Workspace } from '@/lib/workspaces/store'
import { normalizeWorkspacePath } from '@/lib/workspaces/store'

import { buildChatTree, filterChatTreeNodes, type ChatTreeNode } from './chat-tree'

export type WorkspaceChatGroup = {
  id: string
  name: string
  path: string
  isOrphan: boolean
  chatNodes: ChatTreeNode[]
}

export const ORPHAN_GROUP_ID = '__orphan__'

export function groupChatsByWorkspace(
  workspaces: Workspace[],
  chats: ChatThread[]
): WorkspaceChatGroup[] {
  const matchedChatIds = new Set<string>()
  const groups: WorkspaceChatGroup[] = []

  const sortedWorkspaces = [...workspaces].sort((a, b) => a.name.localeCompare(b.name))

  for (const workspace of sortedWorkspaces) {
    const normalizedPath = normalizeWorkspacePath(workspace.path)
    const workspaceChats = chats.filter((chat) => {
      if (normalizeWorkspacePath(chat.workspacePath) !== normalizedPath) {
        return false
      }

      matchedChatIds.add(chat.id)
      return true
    })

    groups.push({
      id: workspace.id,
      name: workspace.name,
      path: workspace.path,
      isOrphan: false,
      chatNodes: buildChatTree(workspaceChats)
    })
  }

  const orphanChats = chats.filter((chat) => !matchedChatIds.has(chat.id))

  if (orphanChats.length > 0) {
    groups.push({
      id: ORPHAN_GROUP_ID,
      name: 'Other',
      path: '',
      isOrphan: true,
      chatNodes: buildChatTree(orphanChats)
    })
  }

  return groups
}

export function filterWorkspaceGroups(
  groups: WorkspaceChatGroup[],
  searchQuery: string
): WorkspaceChatGroup[] {
  const query = searchQuery.trim().toLowerCase()
  if (!query) {
    return groups
  }

  return groups
    .map((group) => ({
      ...group,
      chatNodes: filterChatTreeNodes(group.chatNodes, query)
    }))
    .filter((group) => group.chatNodes.length > 0)
}
