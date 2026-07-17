import type { ChatStatus, ChatThread } from '@/lib/chat/types'

export type KanbanColumnId = 'processing' | 'readyToRead' | 'done'

export type KanbanColumn = {
  id: KanbanColumnId
  title: string
  status: ChatStatus
  chats: ChatThread[]
}

const COLUMN_DEFS: ReadonlyArray<{
  id: KanbanColumnId
  title: string
  status: ChatStatus
}> = [
  { id: 'processing', title: 'Processing', status: 'inProgress' },
  { id: 'readyToRead', title: 'Ready to Read', status: 'readyForReview' },
  { id: 'done', title: 'Done', status: 'read' }
]

function compareByUpdatedAtDesc(a: ChatThread, b: ChatThread): number {
  return b.updatedAt.localeCompare(a.updatedAt)
}

type GroupChatsByStatusOptions = {
  resolveStatus?: (chat: ChatThread) => ChatStatus
}

/** Groups chats into Kanban columns by server status (SSE-driven). */
export function groupChatsByStatus(
  chats: ChatThread[],
  options: GroupChatsByStatusOptions = {}
): KanbanColumn[] {
  const byStatus = new Map<ChatStatus, ChatThread[]>()
  const resolveStatus = options.resolveStatus ?? ((chat) => chat.status)

  for (const chat of chats) {
    const status = resolveStatus(chat)
    const bucket = byStatus.get(status) ?? []
    bucket.push(chat)
    byStatus.set(status, bucket)
  }

  return COLUMN_DEFS.map((def) => ({
    id: def.id,
    title: def.title,
    status: def.status,
    chats: (byStatus.get(def.status) ?? []).sort(compareByUpdatedAtDesc)
  }))
}
