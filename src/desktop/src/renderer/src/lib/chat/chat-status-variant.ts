import type { ChatStatus, ChatThread } from '@/lib/chat/types'

export type ChatStatusVariant = 'standard' | 'active' | 'attention' | 'draft'

type GetChatStatusVariantOptions = {
  chat: ChatThread
  isSending: boolean
  isParentKickingOff: boolean
  /** True when the user is currently viewing this chat (primary or split pane). */
  isViewing?: boolean
}

export function mapChatStatusToVariant(status: ChatStatus): ChatStatusVariant {
  switch (status) {
    case 'inProgress':
      return 'active'
    case 'readyForReview':
      return 'attention'
    default:
      return 'standard'
  }
}

export function getChatStatusVariant({
  chat,
  isSending,
  isParentKickingOff,
  isViewing = false
}: GetChatStatusVariantOptions): ChatStatusVariant {
  if (isSending || isParentKickingOff) {
    return 'active'
  }

  // Still processing on the server — keep amber even while the tab is open.
  if (chat.status === 'inProgress') {
    return 'active'
  }

  // Viewing a finished chat — show read (gray) while mark-read syncs.
  if (isViewing) {
    return 'standard'
  }

  return mapChatStatusToVariant(chat.status)
}

export function getChatStatusVariantLabel(variant: ChatStatusVariant): string | undefined {
  switch (variant) {
    case 'active':
      return 'In progress'
    case 'attention':
      return 'Needs attention'
    case 'draft':
      return 'Draft'
    default:
      return undefined
  }
}
