import type { ChatThread } from '@/lib/chat/types'

export function mergeChatLists(cached: ChatThread[], incoming: ChatThread[]): ChatThread[] {
  const merged = new Map<string, ChatThread>()

  for (const chat of cached) {
    merged.set(chat.id, chat)
  }

  for (const chat of incoming) {
    merged.set(chat.id, chat)
  }

  return Array.from(merged.values()).sort(
    (left, right) => new Date(right.updatedAt).getTime() - new Date(left.updatedAt).getTime()
  )
}
