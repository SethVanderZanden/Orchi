import type { ChatThread } from '@/lib/chat/types'
import type { Project } from '@/lib/projects/types'

export type ChatFinderGroup = {
  id: string
  heading: string
  chats: ChatThread[]
}

export const RECENT_CHATS_LIMIT = 8

export function getRecentChats(chats: ChatThread[], limit = RECENT_CHATS_LIMIT): ChatThread[] {
  return chats.slice(0, limit)
}

export function buildChatFinderGroups(chats: ChatThread[], projects: Project[]): ChatFinderGroup[] {
  const groups: ChatFinderGroup[] = []

  const recent = getRecentChats(chats)
  if (recent.length > 0) {
    groups.push({
      id: 'recent',
      heading: 'Recent',
      chats: recent
    })
  }

  const byProjectId = new Map<string, ChatThread[]>()
  const orphans: ChatThread[] = []

  for (const chat of chats) {
    if (!chat.projectId) {
      orphans.push(chat)
      continue
    }

    const existing = byProjectId.get(chat.projectId) ?? []
    existing.push(chat)
    byProjectId.set(chat.projectId, existing)
  }

  const sortedProjects = [...projects].sort((a, b) => a.name.localeCompare(b.name))
  for (const project of sortedProjects) {
    const projectChats = byProjectId.get(project.id)
    if (!projectChats || projectChats.length === 0) {
      continue
    }

    groups.push({
      id: `project:${project.id}`,
      heading: project.name,
      chats: projectChats
    })
  }

  if (orphans.length > 0) {
    groups.push({
      id: 'other',
      heading: 'Other',
      chats: orphans
    })
  }

  return groups
}
