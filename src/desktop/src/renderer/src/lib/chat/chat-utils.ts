import type { ChatMessage, ChatThread } from './types'

export function createChatId(): string {
  return crypto.randomUUID()
}

export function createMessage(role: ChatMessage['role'], content: string): ChatMessage {
  return {
    id: crypto.randomUUID(),
    role,
    content,
    createdAt: new Date()
  }
}

export function createEmptyChat(): ChatThread {
  const now = new Date()

  return {
    id: createChatId(),
    title: 'New chat',
    preview: 'Start a conversation with Orchi',
    updatedAt: now,
    messages: []
  }
}

export function deriveChatTitle(content: string): string {
  const trimmed = content.trim()
  if (!trimmed) {
    return 'New chat'
  }

  return trimmed.length > 42 ? `${trimmed.slice(0, 42)}…` : trimmed
}

export function appendUserMessage(chat: ChatThread, content: string): ChatThread {
  const message = createMessage('user', content)
  const isFirstMessage = chat.messages.length === 0

  return {
    ...chat,
    title: isFirstMessage ? deriveChatTitle(content) : chat.title,
    preview: content,
    updatedAt: message.createdAt,
    messages: [...chat.messages, message]
  }
}

export function appendAssistantMessage(chat: ChatThread, content: string): ChatThread {
  const message = createMessage('assistant', content)

  return {
    ...chat,
    preview: content,
    updatedAt: message.createdAt,
    messages: [...chat.messages, message]
  }
}
