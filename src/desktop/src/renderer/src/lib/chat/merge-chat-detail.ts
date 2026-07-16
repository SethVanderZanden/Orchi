import type { ChatMessage, ChatThread } from '@/lib/chat/types'

import { mergeChatThread } from '@/lib/chat/merge-chat-lists'

function isInFlightAssistant(message: ChatMessage): boolean {
  return (
    message.role === 'assistant' &&
    (message.status === 'processing' || message.status === 'streaming')
  )
}

function hasInFlightAssistant(messages: ChatMessage[]): boolean {
  const last = messages.at(-1)
  return last !== undefined && isInFlightAssistant(last)
}

export function mergeChatDetail(
  existing: ChatThread | undefined,
  incoming: ChatThread
): ChatThread {
  if (!existing) {
    return incoming
  }

  if (hasInFlightAssistant(existing.messages)) {
    if (incoming.messages.length < existing.messages.length) {
      return {
        ...mergeChatThread(existing, incoming),
        messages: existing.messages
      }
    }

    const incomingLast = incoming.messages.at(-1)
    if (incomingLast && !isInFlightAssistant(incomingLast)) {
      return mergeChatThread(existing, incoming)
    }

    return {
      ...mergeChatThread(existing, incoming),
      messages: existing.messages
    }
  }

  if (existing.messages.length > incoming.messages.length) {
    return {
      ...mergeChatThread(existing, incoming),
      messages: existing.messages
    }
  }

  return mergeChatThread(existing, incoming)
}
