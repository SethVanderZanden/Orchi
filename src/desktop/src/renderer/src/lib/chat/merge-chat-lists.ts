import { mergeChatStatus } from '@/lib/chat/prefer-chat-status'
import type { ChatThread } from '@/lib/chat/types'

/** List cache entries never retain message history — detail queries own messages. */
export function toListSummary(chat: ChatThread): ChatThread {
  const lastMessage = chat.messages.at(-1)

  return {
    ...chat,
    preview: chat.preview || lastMessage?.content || 'Start a conversation with Orchi',
    updatedAt: lastMessage?.createdAt ?? chat.updatedAt,
    messages: []
  }
}

export function mergeChatThread(existing: ChatThread, incoming: ChatThread): ChatThread {
  const messages = incoming.messages.length > 0 ? incoming.messages : existing.messages

  return {
    ...existing,
    ...incoming,
    messages,
    status: mergeChatStatus(existing.status, incoming.status)
  }
}

/** Merge list/summary rows only — strips any message history from the result. */
export function mergeListThread(existing: ChatThread, incoming: ChatThread): ChatThread {
  const merged = mergeChatThread(existing, incoming)
  const lastMessage = incoming.messages.at(-1) ?? existing.messages.at(-1)

  return {
    ...merged,
    preview:
      incoming.preview ||
      existing.preview ||
      lastMessage?.content ||
      'Start a conversation with Orchi',
    updatedAt: lastMessage?.createdAt ?? incoming.updatedAt ?? existing.updatedAt,
    messages: []
  }
}

export function mergeChatLists(cached: ChatThread[], incoming: ChatThread[]): ChatThread[] {
  const merged = new Map<string, ChatThread>()

  for (const chat of cached) {
    merged.set(chat.id, toListSummary(chat))
  }

  for (const chat of incoming) {
    const existing = merged.get(chat.id)
    merged.set(chat.id, existing ? mergeListThread(existing, chat) : toListSummary(chat))
  }

  return Array.from(merged.values()).sort(
    (left, right) => new Date(right.updatedAt).getTime() - new Date(left.updatedAt).getTime()
  )
}
