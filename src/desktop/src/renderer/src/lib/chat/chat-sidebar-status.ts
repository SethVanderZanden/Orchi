import type { ChatStatus, ChatThread } from '@/lib/chat/types'

export type ChatSidebarStatusVariant = 'standard' | 'active' | 'attention'

type GetChatSidebarStatusOptions = {
  chat: ChatThread
  isSending: boolean
  isParentKickingOff: boolean
}

export function mapChatStatusToSidebarVariant(status: ChatStatus): ChatSidebarStatusVariant {
  switch (status) {
    case 'inProgress':
      return 'active'
    case 'readyForReview':
      return 'attention'
    default:
      return 'standard'
  }
}

export function getChatSidebarStatus({
  chat,
  isSending,
  isParentKickingOff
}: GetChatSidebarStatusOptions): ChatSidebarStatusVariant {
  if (isSending || isParentKickingOff) {
    return 'active'
  }

  return mapChatStatusToSidebarVariant(chat.status)
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
