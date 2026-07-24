import type { ChatStatus, ChatThread } from '@/lib/chat/types'
import { groupChatsByStatus, type KanbanColumn } from '@/lib/kanban/group-chats-by-status'
import type { BoardGroupingMode } from '@/lib/preferences/board-grouping'
import { ORPHAN_GROUP_ID } from '@/lib/projects/group-chats'
import type { Project } from '@/lib/projects/types'

export type BoardStatusSection = KanbanColumn

export type BoardProjectGroup = {
  id: string
  name: string
  isOrphan: boolean
  sections: BoardStatusSection[]
  chatCount: number
}

export type BoardGroupedView =
  | { mode: 'state'; sections: BoardStatusSection[] }
  | { mode: 'project'; projects: BoardProjectGroup[] }

type GroupBoardChatsOptions = {
  grouping: BoardGroupingMode
  projects: Project[]
  resolveStatus?: (chat: ChatThread) => ChatStatus
}

function compareByName(a: { name: string }, b: { name: string }): number {
  return a.name.localeCompare(b.name)
}

function buildProjectGroups(
  chats: ChatThread[],
  projects: Project[],
  resolveStatus?: (chat: ChatThread) => ChatStatus
): BoardProjectGroup[] {
  const matchedChatIds = new Set<string>()
  const groups: BoardProjectGroup[] = []

  const sortedProjects = [...projects].sort(compareByName)

  for (const project of sortedProjects) {
    const projectChats: ChatThread[] = []

    for (const chat of chats) {
      if (matchedChatIds.has(chat.id)) {
        continue
      }

      if (chat.projectId !== project.id) {
        continue
      }

      matchedChatIds.add(chat.id)
      projectChats.push(chat)
    }

    const sections = groupChatsByStatus(projectChats, { resolveStatus })
    const chatCount = sections.reduce((total, section) => total + section.chats.length, 0)

    if (chatCount === 0) {
      continue
    }

    groups.push({
      id: project.id,
      name: project.name,
      isOrphan: false,
      sections,
      chatCount
    })
  }

  const orphanChats = chats.filter((chat) => !matchedChatIds.has(chat.id))
  if (orphanChats.length > 0) {
    const sections = groupChatsByStatus(orphanChats, { resolveStatus })
    groups.push({
      id: ORPHAN_GROUP_ID,
      name: 'Other',
      isOrphan: true,
      sections,
      chatCount: sections.reduce((total, section) => total + section.chats.length, 0)
    })
  }

  return groups
}

/** Groups filtered board chats by state, or by project then state. */
export function groupBoardChats(
  chats: ChatThread[],
  options: GroupBoardChatsOptions
): BoardGroupedView {
  const { grouping, projects, resolveStatus } = options

  if (grouping === 'project') {
    return {
      mode: 'project',
      projects: buildProjectGroups(chats, projects, resolveStatus)
    }
  }

  return {
    mode: 'state',
    sections: groupChatsByStatus(chats, { resolveStatus })
  }
}
