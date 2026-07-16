import type { ChatThread } from '@/lib/chat/types'

export function mergeChatThread(existing: ChatThread, incoming: ChatThread): ChatThread {
  const messages = incoming.messages.length > 0 ? incoming.messages : existing.messages

  return {
    ...existing,
    ...incoming,
    messages
  }
}

export function mergeChatLists(cached: ChatThread[], incoming: ChatThread[]): ChatThread[] {
  const merged = new Map<string, ChatThread>()

  for (const chat of cached) {
    merged.set(chat.id, chat)
  }

  for (const chat of incoming) {
    const existing = merged.get(chat.id)
    merged.set(chat.id, existing ? mergeChatThread(existing, chat) : chat)
  }

  return Array.from(merged.values()).sort(
    (left, right) => new Date(right.updatedAt).getTime() - new Date(left.updatedAt).getTime()
  )
}
