import { getLastReadUpdatedAt, isChatUnread } from '@/lib/chat/chat-read-state'
import type { ChatThread } from '@/lib/chat/types'
import { hasReviewReadyPlan } from '@/lib/orchestration/review-ready'

export type ChatSidebarStatusVariant = 'standard' | 'active' | 'attention'

type GetChatSidebarStatusOptions = {
  chat: ChatThread
  activeChatId?: string
  isSending: boolean
  isParentKickingOff: boolean
  getChat: (chatId: string) => ChatThread | undefined
  getChildChats: (parentChatId: string) => ChatThread[]
}

function hasActiveMessages(chat: ChatThread | undefined): boolean {
  if (!chat) {
    return false
  }

  return chat.messages.some(
    (message) => message.status === 'processing' || message.status === 'streaming'
  )
}

function hasErrorMessage(chat: ChatThread | undefined): boolean {
  if (!chat || chat.messages.length === 0) {
    return false
  }

  const lastMessage = chat.messages[chat.messages.length - 1]
  return lastMessage.status === 'error'
}

export function getChatSidebarStatus({
  chat,
  activeChatId,
  isSending,
  isParentKickingOff,
  getChat,
  getChildChats
}: GetChatSidebarStatusOptions): ChatSidebarStatusVariant {
  const resolvedChat = getChat(chat.id) ?? chat

  if (
    isSending ||
    isParentKickingOff ||
    hasActiveMessages(resolvedChat)
  ) {
    return 'active'
  }

  if (
    hasErrorMessage(resolvedChat) ||
    hasReviewReadyPlan(resolvedChat, getChildChats(chat.id), getChat) ||
    isChatUnread(chat, activeChatId)
  ) {
    return 'attention'
  }

  return 'standard'
}

export function getChatSidebarStatusLabel(variant: ChatSidebarStatusVariant): string | undefined {
  switch (variant) {
    case 'active':
      return 'In progress'
    case 'attention':
      return 'Needs attention'
    default:
      return undefined
  }
}

export { getLastReadUpdatedAt }
