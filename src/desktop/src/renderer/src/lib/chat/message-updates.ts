import type { ChatMessage, ChatThread } from '@/lib/chat/types'

export function appendUserAndAssistantMessages(
  chat: ChatThread,
  content: string,
  assistantMessageId: string
): ChatThread {
  const now = new Date().toISOString()
  const userMessage: ChatMessage = {
    id: crypto.randomUUID(),
    role: 'user',
    content,
    createdAt: now,
    status: 'complete'
  }

  const assistantMessage: ChatMessage = {
    id: assistantMessageId,
    role: 'assistant',
    content: '',
    createdAt: now,
    status: 'processing'
  }

  return {
    ...chat,
    title: chat.messages.length === 0 ? content.slice(0, 42) : chat.title,
    preview: content,
    updatedAt: now,
    messages: [...chat.messages, userMessage, assistantMessage]
  }
}

export function applyToken(message: ChatMessage, text: string): ChatMessage {
  return {
    ...message,
    content: message.content + text,
    status: 'streaming'
  }
}

export function finalizeAssistantMessages(messages: ChatMessage[]): {
  messages: ChatMessage[]
  changed: boolean
} {
  let changed = false
  const next = messages.map((message) => {
    if (
      message.role === 'assistant' &&
      (message.status === 'processing' || message.status === 'streaming')
    ) {
      changed = true
      return { ...message, status: 'complete' as const }
    }

    return message
  })

  return { messages: next, changed }
}

export function updateMessageInThread(
  chat: ChatThread,
  messageId: string,
  updater: (message: ChatMessage) => ChatMessage
): ChatThread {
  const messages = [...chat.messages]
  const index = messages.findIndex((message) => message.id === messageId)
  if (index === -1) {
    return chat
  }

  messages[index] = updater(messages[index])

  return {
    ...chat,
    messages,
    preview: messages[index].content || chat.preview,
    updatedAt: new Date().toISOString()
  }
}

export function isAbortError(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError'
}
